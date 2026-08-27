using Autodesk.AutoCAD.Interop;
using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.Ipc;
using AutoGala.Services;
using AutoGala.ViewModels.Base;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace AutoGala.ViewModels
{
    public class AutoGalaProcessSelectionViewModel : ViewModelBase
    {
        private const string TargetProcessName = "acad";

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
        public ICommand SelectCommand { get; }

        public AutoGalaProcessSelectionViewModel(IAutoGalaProcessService autoGalaProcessService) 
        {
            _autoGalaProcessService = autoGalaProcessService;

            SelectCommand = new RelayCommand(async param => await Select(), param => SelectedInstance != null);

            Refresh();
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

        private async Task Select()
        {
            if (SelectedInstance == null)
            {
                return;
            }

            int pid = SelectedInstance.ProcessId;

            AcadApplication acadApp = _autoGalaProcessService.GetAcadApplicationByProcessId(pid);

            if (acadApp == null)
            {
                Debug.WriteLine("Could not find autoCAD instance.");
                return;
            }
            // when deployed
            //string pluginPath = @"D:\Programming\Job\software and tests\AutoGala\AutoGala.Plugin.dll";

            // load plugin into autoCAD instance. Skip if autoloaded.
            string pluginPath = @"D:\Programming\Job\AutoGala\AutoGala.Plugin\bin\Debug\net10.0-windows\AutoGala.Plugin.dll";
            acadApp.ActiveDocument.SendCommand(
                "(setvar \"FILEDIA\" 0)\n" +
                $"NETLOAD \"{pluginPath}\"\n" +
                "(setvar \"FILEDIA\" 1)\n");

            var client = new NamedPipeClientStream(".", $"AutoGala_{pid}", PipeDirection.InOut);
            await client.ConnectAsync(3000);

            using var writer = new StreamWriter(client) { AutoFlush = true };
            using var reader = new StreamReader(client);

            var request = new PluginRequest
            {
                Action = "AddLine",
                PayloadJson = """
                {
                    "Start": { "X": 71, "Y": 0, "Z": 0 },
                    "End":   { "X": 63, "Y": 0, "Z": 0 }
                }
                """
            };

            await writer.WriteLineAsync(JsonSerializer.Serialize(request));
            string? responseLine = await reader.ReadLineAsync();

            var response = JsonSerializer.Deserialize<PluginResponse>(responseLine!);

            if (response!.Success)
            {

            }
            else
            {
                throw new InvalidOperationException(response.Error);
            }
        }
    }
}
