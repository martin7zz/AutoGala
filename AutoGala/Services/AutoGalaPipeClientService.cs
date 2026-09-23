using AutoGala.Contracts;
using AutoGala.Ipc;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;

using static AutoGala.Common.UiNavigation;

namespace AutoGala.Services
{
    public class AutoGalaPipeClientService : IAutoGalaPipeClientService
    {
        private const int ConnectAttempts = 5;
        private const int ConnectAttemptTimeoutMs = 1000;
        private const int ConnectRetryDelayMs = 300;
        private const string NotConnectedMessage =
            "Not connected to AutoCAD. Press the connect to autoCAD button in the menu.";

        private NamedPipeClientStream? _pipe;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private Process? _watchedProcess;

        // One request in flight at a time: the protocol is strictly request -> response.
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
                await ConnectWithRetryAsync(pipe, pid, ct);
            }
            catch
            {
                pipe.Dispose();
                throw;
            }

            // Use our own Process instance so nobody else (e.g. the process list view model)
            // can dispose it out from under the connection.
            Process watched;
            try
            {
                watched = Process.GetProcessById(pid);
                watched.EnableRaisingEvents = true;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                pipe.Dispose();
                throw new InvalidOperationException("AutoCAD exited while connecting.", ex);
            }

            _pipe = pipe;
            _reader = new StreamReader(pipe, leaveOpen: true);
            _writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            _watchedProcess = watched;

            watched.Exited += OnAutoCADExited;

            // Exited won't fire if the process died before the handler was attached.
            if (watched.HasExited)
            {
                Disconnect();
                throw new InvalidOperationException("AutoCAD exited while connecting.");
            }

            RaiseConnectionStateChanged();
        }

        private static async Task ConnectWithRetryAsync(NamedPipeClientStream pipe, int pid, CancellationToken ct)
        {
            for (int attempt = 1; attempt <= ConnectAttempts; attempt++)
            {
                try
                {
                    await pipe.ConnectAsync(ConnectAttemptTimeoutMs, ct);
                    return;
                }
                catch (TimeoutException) when (attempt < ConnectAttempts)
                {
                    await Task.Delay(ConnectRetryDelayMs, ct);
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            throw new InvalidOperationException($"Could not connect to AutoGala_{pid}");
        }

        public async Task<PluginResponse> SendAsync(PluginRequest request, CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                // Check after taking the lock: state may have changed while we were queued.
                var writer = _writer;
                var reader = _reader;
                if (!IsConnected || writer is null || reader is null)
                    throw new InvalidOperationException(NotConnectedMessage);

                await writer.WriteLineAsync(JsonSerializer.Serialize(request));

                // No timeout: commands can wait on the user, who cancels them in AutoCAD as usual.
                string line = await reader.ReadLineAsync(ct)
                    ?? throw new IOException("Pipe was closed by AutoCAD.");

                return JsonSerializer.Deserialize<PluginResponse>(line)
                    ?? throw new InvalidOperationException("Bad response from plugin.");
            }
            catch (OperationCanceledException)
            {
                // The request is still in flight on the plugin side. A late reply would be
                // read as the answer to the next request, so drop the connection.
                Disconnect();
                throw;
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

        // Process.Exited fires on a thread-pool thread. Move off the callback before
        // disposing the Process it came from; the state event marshals itself to the UI thread.
        private void OnAutoCADExited(object? sender, EventArgs e) => Task.Run(Disconnect);

        // Safe to call repeatedly and from any thread. Disposing the pipe also unblocks
        // a pending read in SendAsync, which then fails with IOException/ObjectDisposedException.
        public void Disconnect()
        {
            var proc = Interlocked.Exchange(ref _watchedProcess, null);
            var pipe = Interlocked.Exchange(ref _pipe, null);
            _reader = null;
            _writer = null;

            if (proc is null && pipe is null)
                return;

            if (proc is not null)
            {
                proc.Exited -= OnAutoCADExited;
                proc.Dispose();
            }

            // The reader/writer were created with leaveOpen: true and own nothing.
            // Disposing them would flush into a possibly broken pipe and throw.
            try { pipe?.Dispose(); }
            catch (IOException) { }
            catch (ObjectDisposedException) { }

            RaiseConnectionStateChanged();
        }

        private void RaiseConnectionStateChanged()
        {
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher is null || dispatcher.CheckAccess())
                ConnectionStateChanged?.Invoke();
            else
                dispatcher.BeginInvoke(() => ConnectionStateChanged?.Invoke());
        }

        public ValueTask DisposeAsync()
        {
            Disconnect();
            _lock.Dispose();
            return ValueTask.CompletedTask;
        }

        public void ActivateAutoCAD()
        {
            var process = _watchedProcess
                ?? throw new InvalidOperationException("AutoCAD is not connected.");

            if (process.HasExited)
            {
                Disconnect();
                throw new InvalidOperationException("AutoCAD has exited.");
            }

            process.Refresh();
            var handle = process.MainWindowHandle;

            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Could not find the AutoCAD window.");

            // Only restore when minimized: SW_RESTORE on a maximized window would un-maximize it.
            if (IsIconic(handle))
                ShowWindow(handle, SW_RESTORE);

            SetForegroundWindow(handle);
        }

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}