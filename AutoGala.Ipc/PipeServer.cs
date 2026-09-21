using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using System.Text;

namespace AutoGala.Ipc
{
    public class PipeServer
    {
        private readonly string _pipeName;
        private CancellationTokenSource _cts;
        private Task? _listenTask;

        // Handler receives the raw request line, returns the raw response line.
        public Func<string, Task<string>>? RequestHandler { get; set; }

        public PipeServer(string pipeName)
        {
            _pipeName = pipeName;
        }

        public void Start()
        {
            if (_listenTask != null)
                return;

            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (_cts == null)
                return;

            _cts.Cancel();

            try
            {
                if (_listenTask != null)
                    await _listenTask;
            }
            catch (OperationCanceledException)
            {
            }

            _cts.Dispose();
            _cts = null;
            _listenTask = null;
        }
        public Action<string>? Log { get; set; }
        private async Task ListenLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    using var pipe = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    try
                    {
                        await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);

                        using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
                        using var writer = new StreamWriter(pipe, Encoding.UTF8, 1024, leaveOpen: true) { AutoFlush = true };

                        // keep connected for multiple messages or only one.
                        while (pipe.IsConnected &&
                               !token.IsCancellationRequested)
                        {
#if NET7_0_OR_GREATER
                            string? line = await reader.ReadLineAsync(token);
#else
                            // StreamReader.ReadLineAsync has no CancellationToken overload pre-.NET 7.
                            // Registering a callback that closes the pipe forces the blocked read to
                            // throw (IOException/ObjectDisposedException) when the token cancels.
                            using var registration = token.Register(() =>
                            {
                                try { pipe.Close(); } catch { /* already closing */ }
                            });

                            string? line = await reader.ReadLineAsync().ConfigureAwait(false);
#endif
                            if (line == null)
                            {
                                break;
                            }

                            string response = RequestHandler != null
                                ? await RequestHandler(line).ConfigureAwait(false) : JsonSerializer.Serialize(new PluginResponse { Success = false, Error = "No handler registered" });

                            await writer.WriteLineAsync(response).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Stop was called.");
                    }
                    catch (IOException ex)
                    {
                        Debug.WriteLine(
                            $"Pipe client disconnected: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log?.Invoke($"PipeServer loop died: {ex}");
                Debug.WriteLine($"PipeServer loop died: {ex}");
                throw;
            }
            finally { Debug.WriteLine("Pipe server stopped."); }
        }
    }
}
