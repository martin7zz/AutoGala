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

namespace AutoGala.ViewModels
{
    public class AutoGalaProcessSelectionViewModel : ViewModelBase, IDisposable
    {
        private const string TargetProcessName = "acad";

        private ManagementEventWatcher? _processStartWatcher;
        private ManagementEventWatcher? _processStopWatcher;

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

        private readonly IAutoGalaProcessService _autoGalaProcessService;
        private readonly IAutoGalaPipeClientService _pipeClientService;

        public ICommand SelectCommand { get; }

        public ICommand RefreshCommand { get; }

        public AutoGalaProcessSelectionViewModel(IAutoGalaProcessService autoGalaProcessService,
            IAutoGalaPipeClientService autoGalaPipeClientService)
        {
            _autoGalaProcessService = autoGalaProcessService;
            _pipeClientService = autoGalaPipeClientService;

            SelectCommand = new RelayCommand(async param => await Select(), param => SelectedInstance != null);
            RefreshCommand = new RelayCommand(param => Refresh());

            Refresh();

            StartProcessWatcher();
        }

        private void Refresh()
        {
            RunningInstances.Clear();

            foreach (var process in Process.GetProcessesByName(TargetProcessName))
            {
                try
                {
                    RunningInstances.Add(new AutoCADApplication
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        WindowTitle = process.MainWindowTitle,
                        Process = process
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    throw new InvalidOperationException();
                }
                Debug.WriteLine($"Found {RunningInstances.Count} instances");
            }
        }

        private void StartProcessWatcher()
        {
            _processStartWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));

            _processStartWatcher.EventArrived += (_, e) =>
            {
                var processName = e.NewEvent["ProcessName"]?.ToString();

                if (string.Equals(processName, "acad.exe", StringComparison.OrdinalIgnoreCase))
                {
                    Application.Current.Dispatcher.Invoke(Refresh);
                }
            };

            _processStartWatcher.Start();

            _processStopWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));

            _processStopWatcher.EventArrived += (_, e) =>
            {
                var processName = e.NewEvent["ProcessName"]?.ToString();

                if (string.Equals(processName, "acad.exe", StringComparison.OrdinalIgnoreCase))
                {
                    Application.Current.Dispatcher.Invoke(Refresh);
                }
            };

            _processStopWatcher.Start();
        }

        public void Dispose()
        {
            _processStartWatcher?.Stop();
            _processStartWatcher?.Dispose();

            _processStopWatcher?.Stop();
            _processStopWatcher?.Dispose();
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
            catch (InvalidOperationException)
            {
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
