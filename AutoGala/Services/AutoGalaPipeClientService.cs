using AutoGala.Contracts;
using AutoGala.Ipc;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Windows;

using static AutoGala.Common.UiNavigation;

namespace AutoGala.Services
{
    public class AutoGalaPipeClientService : IAutoGalaPipeClientService
    {
        private NamedPipeClientStream? _pipe;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private Process? _watchedProcess;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public event Action? ConnectionStateChanged;

        public bool IsConnected => _pipe?.IsConnected ?? false;

        public async Task ConnectAsync(Process process, CancellationToken ct = default)
        {
            if (IsConnected) return;

            int pid = process.Id;
            var pipe = new NamedPipeClientStream(".", $"AutoGala_{pid}", PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                const int maxAttempts = 5;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        await pipe.ConnectAsync(1000, ct);
                        break;
                    }
                    catch (TimeoutException)
                    {
                        if (attempt == maxAttempts)
                            break;
                        await Task.Delay(300, ct);
                    }
                }

                if (!pipe.IsConnected)
                    throw new InvalidOperationException($"Could not connect to AutoGala_{pid}");
            }
            catch
            {
                pipe.Dispose();
                throw;
            }

            _pipe = pipe;
            _reader = new StreamReader(_pipe, leaveOpen: true);
            _writer = new StreamWriter(_pipe, leaveOpen: true) { AutoFlush = true };

            _watchedProcess = process;
            _watchedProcess.EnableRaisingEvents = true;
            _watchedProcess.Exited += OnAutoCADExited;

            ConnectionStateChanged?.Invoke();
        }

        // Process.Exited fires on a ThreadPool thread — hop back to the UI thread
        // before anything downstream (CommandManager.InvalidateRequerySuggested) runs.
        private void OnAutoCADExited(object? sender, EventArgs e)
        {
            Application.Current?.Dispatcher.BeginInvoke(Disconnect);
        }

        public void Disconnect()
        {
            var proc = _watchedProcess;
            _watchedProcess = null;
            if (proc != null)
                proc.Exited -= OnAutoCADExited;

            var pipe = _pipe;
            _pipe = null;
            _reader = null;
            _writer = null;

            try { pipe?.Dispose(); }
            catch (IOException) { }
            catch (ObjectDisposedException) { }

            ConnectionStateChanged?.Invoke();
        }

        public async ValueTask DisposeAsync()
        {
            Disconnect();
            _lock.Dispose();
            await Task.CompletedTask;
        }

        public async Task<PluginResponse> SendAsync(PluginRequest request, CancellationToken ct = default)
        {
            if (!IsConnected || _writer is null || _reader is null)
            {
                throw new InvalidOperationException("Not connected to AutoCAD. Press the connect to autoCAD button in the menu.");
            }

            await _lock.WaitAsync(ct);
            try
            {
                var writer = _writer;
                var reader = _reader;
                if (!IsConnected || writer is null || reader is null)
                    throw new InvalidOperationException("Not connected to AutoCAD. Press the connect to autoCAD button in the menu.");

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                try
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(request));
                    string? line = await reader.ReadLineAsync(timeoutCts.Token);

                    if (line is null)
                    {
                        Disconnect();
                        throw new IOException("Pipe was closed by AutoCAD.");
                    }

                    return JsonSerializer.Deserialize<PluginResponse>(line)
                        ?? throw new InvalidOperationException("Bad response from plugin.");
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // A late reply would be read as the answer to the *next* request,
                    // so drop the connection instead of reusing it.
                    Disconnect();
                    throw new TimeoutException("AutoCAD did not respond in time.");
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                Disconnect();
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void ActivateAutoCAD()
        {
            var process = _watchedProcess;

            if (process == null)
                throw new InvalidOperationException("AutoCAD is not connected.");

            try
            {
                if (process.HasExited)
                {
                    Disconnect();
                    throw new InvalidOperationException("AutoCAD has exited.");
                }

                process.Refresh();

                var handle = process.MainWindowHandle;

                if (handle == IntPtr.Zero)
                    throw new InvalidOperationException("Could not find the AutoCAD window.");

                // If AutoCAD is minimized, restore it first.
                ShowWindow(handle, SW_RESTORE);

                SetForegroundWindow(handle);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Could not bring AutoCAD to the foreground.",
                    ex);
            }
        }
    }
}
