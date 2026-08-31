using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

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
            _listenTask = ListenLoopAsync(_cts.Token);
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
                        await pipe.WaitForConnectionAsync(token);

                        using var reader = new StreamReader(pipe, leaveOpen: true);
                        using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

                        // keep connected for multiple messages or only one.
                        while (pipe.IsConnected &&
                               !token.IsCancellationRequested)
                        {
                            string? line = await reader.ReadLineAsync(token);
                            if (line == null)
                            {
                                break;
                            }

                            string response = RequestHandler != null
                                ? await RequestHandler(line) : JsonSerializer.Serialize(new PluginResponse { Success = false, Error = "No handler registered" });

                            await writer.WriteLineAsync(response);
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
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Debug.WriteLine("Pipe server stopped.");
            }
        }
    }
}
