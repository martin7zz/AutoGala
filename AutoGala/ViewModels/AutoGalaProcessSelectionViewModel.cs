using Autodesk.AutoCAD.Interop;
using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
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
                if (!await TryConnectAsync(process))
                    await LoadPluginAndConnectAsync(process);

                ConnectionSucceeded?.Invoke();
            }
            catch (Exception ex)
            {
                ConnectionFailed?.Invoke(ex.Message);
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
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError(ex.Message);
                return false;
            }
        }

        private async Task LoadPluginAndConnectAsync(Process process)
        {
            AcadApplication? acadApp = await Task.Run(
                () => _autoGalaProcessService.GetAcadApplicationByProcessId(process.Id));

            try
            {
                InjectPlugin(acadApp);
            }
            finally
            {
                Marshal.ReleaseComObject(acadApp);
            }

            await _pipeClientService.ConnectAsync(process);
        }

        private static void InjectPlugin(AcadApplication acadApp)
        {
            const string pluginPath = @"D:\Programming\Job\AutoGala\AutoGala.Plugin\bin\Debug\net10.0-windows\AutoGala.Plugin.dll";

            AcadDocument? document = acadApp.ActiveDocument;
            try
            {
                document.SendCommand(
                    "(setvar \"FILEDIA\" 0)\n" +
                    $"NETLOAD \"{pluginPath}\"\n" +
                    "(setvar \"FILEDIA\" 1)\n");
            }
            finally
            {
                Marshal.ReleaseComObject(document);
            }
        }
    }
}
