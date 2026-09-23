using AutoGala.Contracts;
using AutoGala.Ipc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;

namespace AutoGala.Services
{
    public class AutoCADStateService : IAutoCADStateService, IDisposable
    {
        private readonly IAutoGalaPipeClientService _pipeClientService;
        private readonly DispatcherTimer _timer;
        private bool _hasActiveDocument;

        public bool HasActiveDocument
        {
            get => _hasActiveDocument;
            private set
            {
                if (_hasActiveDocument == value) return;
                _hasActiveDocument = value;
                StateChanged?.Invoke();
            }
        }

        public event Action? StateChanged;

        public AutoCADStateService(IAutoGalaPipeClientService pipeClientService) 
        {
            _pipeClientService = pipeClientService;
            _pipeClientService.ConnectionStateChanged += OnConnectionStateChanged;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
            _timer.Tick += async (_, _) => await PollAsync();
            _timer.Start();
        }

        private void OnConnectionStateChanged()
        {
            if (!_pipeClientService.IsConnected)
                HasActiveDocument = false;
        }
        private async Task PollAsync()
        {
            if (!_pipeClientService.IsConnected) return;

            try
            {
                var response = await _pipeClientService.SendAsync(new PluginRequest { Action = "GetActiveDocumentState" });

                if (!response.Success)
                {
                    HasActiveDocument = false;
                    return;
                }

                using var doc = JsonDocument.Parse(response.ResultJson);
                HasActiveDocument = doc.RootElement.TryGetProperty("HasActiveDocument", out var el) && el.GetBoolean();
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or JsonException or KeyNotFoundException or OperationCanceledException)
            {
                Debug.WriteLine(ex);
                HasActiveDocument = false;
            }
        }

        public void Dispose() => _timer.Stop();
    }
}
