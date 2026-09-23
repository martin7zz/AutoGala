using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AutoGala.ViewModels
{
    public class AutoGalaProcessSelectionViewModel : ViewModelBase, IDisposable
    {
        private const string TargetProcessName = "acad";

        private DispatcherTimer? _processUpdateTimer;

        public ObservableCollection<AutoCADApplication> RunningInstances { get; } = new();

        private AutoCADApplication? _selectedInstance;

        public AutoCADApplication? SelectedInstance
        {
            get => _selectedInstance;
            set
            {
                _selectedInstance = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private readonly IWindowService _windowService;
        private readonly IAutoGalaProcessService _autoGalaProcessService;
        private readonly IAutoGalaPipeClientService _pipeClientService;

        public ICommand SelectCommand { get; }

        public ICommand RefreshCommand { get; }

        public AutoGalaProcessSelectionViewModel(IAutoGalaProcessService autoGalaProcessService,
            IAutoGalaPipeClientService autoGalaPipeClientService,
            IWindowService windowService)
        {
            _autoGalaProcessService = autoGalaProcessService;
            _pipeClientService = autoGalaPipeClientService;
            _windowService = windowService;

            SelectCommand = new RelayCommand(async param => await Select(), param => SelectedInstance != null);
            RefreshCommand = new RelayCommand(param => Refresh());

            Refresh();

            StartProcessWatcher();
        }

        private void Refresh()
        {
            try
            {
                var processes = Process.GetProcessesByName(TargetProcessName);

                var processIds = processes
                    .Select(x => x.Id)
                    .ToHashSet();

                // Add/update
                foreach (var process in processes)
                {
                    var instance = RunningInstances
                        .FirstOrDefault(x => x.ProcessId == process.Id);

                    if (instance == null)
                    {
                        RunningInstances.Add(new AutoCADApplication
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowTitle = process.MainWindowTitle,
                            Process = process
                        });

                        continue;
                    }

                    if (instance.WindowTitle != process.MainWindowTitle ||
                        instance.ProcessName != process.ProcessName)
                    {
                        var index = RunningInstances.IndexOf(instance);
                        var wasSelected = SelectedInstance == instance;
                        var oldProcess = instance.Process;

                        RunningInstances[index] = new AutoCADApplication
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowTitle = process.MainWindowTitle,
                            Process = process
                        };

                        if (wasSelected)
                        {
                            SelectedInstance = RunningInstances[index];
                        }

                        oldProcess?.Dispose();
                    }
                }

                // Remove processes that no longer exist
                foreach (var instance in RunningInstances.ToList())
                {
                    if (!processIds.Contains(instance.ProcessId))
                    {
                        if (SelectedInstance == instance)
                            SelectedInstance = null;

                        RunningInstances.Remove(instance);
                        instance.Process?.Dispose();
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError(ex.Message);
            }
        }

        private void StartProcessWatcher()
        {
            // watches for changes that happen to exsiting processes
            _processUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _processUpdateTimer.Tick += (_, _) => Refresh();

            _processUpdateTimer.Start();
        }

        public void Dispose()
        {
            _processUpdateTimer?.Stop();
        }

        public event Action? ConnectionSucceeded;
        public event Action<string>? ConnectionFailed;

        private async Task Select()
        {
            if (SelectedInstance == null) return;
            var process = SelectedInstance.Process;

            try
            {
                if (_pipeClientService.IsConnected)
                {
                    ConnectionSucceeded?.Invoke();
                    return;
                }

                using var loading = _windowService.ShowLoading("Connecting to AutoCAD...");

                if (!await TryConnectAsync(process))
                {
                    loading.Message = "Loading AutoGala plugin...";

                    await Task.Yield();

                    await LoadPluginAndConnectAsync(process);
                }

                ConnectionSucceeded?.Invoke();
            }
            catch (Exception)
            {
                ConnectionFailed?.Invoke("Failed to connect to AutoCAD.");
            }
        }

        private async Task<bool> TryConnectAsync(Process process)
        {
            if (_pipeClientService.IsConnected) return true;
            try
            {
                await _pipeClientService.ConnectAsync(process);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private async Task LoadPluginAndConnectAsync(Process process)
        {
            object? acadApp = await Task.Run(
                () => _autoGalaProcessService.GetAcadApplicationByProcessId(process.Id));

            if (acadApp is null)
                throw new InvalidOperationException("Could not find the AutoCAD instance in the ROT.");

            try
            {
                InjectPlugin(acadApp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            //finally
            //{
            //    Marshal.ReleaseComObject(acadApp);
            //}

            await _pipeClientService.ConnectAsync(process);
        }

        private static readonly string PluginBinRoot = Path.Combine(AppContext.BaseDirectory, "Plugin");

        private static void InjectPlugin(object acadApp)
        {
            dynamic app = acadApp;
            object? document = app.ActiveDocument;
            try
            {
                string acadVer = ((dynamic)document).GetVariable("ACADVER");
                string pluginPath = GetPluginPathForAcadVersion(acadVer);
                ((dynamic)document).SendCommand(
                    "(setvar \"FILEDIA\" 0)\n" +
                    $"NETLOAD \"{pluginPath}\"\n" +
                    "(setvar \"FILEDIA\" 1)\n");
            }
            finally
            {
                Marshal.ReleaseComObject(document);
            }
        }

        private static string GetPluginPathForAcadVersion(string acadVer)
        {
            var match = Regex.Match(acadVer, @"^(\d+)\.(\d+)");
            if (!match.Success)
                throw new InvalidOperationException($"Could not parse ACADVER: '{acadVer}'");

            int major = int.Parse(match.Groups[1].Value);

            string tfm = major switch
            {
                24 => "net48",              // AutoCAD 2021–2024
                25 => "net8.0-windows",     // AutoCAD 2025–2026
                26 => "net10.0-windows",    // AutoCAD 2027
                _ => throw new NotSupportedException($"Unsupported AutoCAD version: {acadVer}")
            };

            string pluginPath = Path.Combine(PluginBinRoot, tfm, "AutoGala.Plugin.dll");

            if (!File.Exists(pluginPath))
                throw new FileNotFoundException(
                    $"Plugin build for AutoCAD series {major}.x not found. Expected at: {pluginPath}", pluginPath);

            return pluginPath;
        }
    }
}
