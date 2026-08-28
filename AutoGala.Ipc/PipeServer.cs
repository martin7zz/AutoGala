using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace AutoGala.Ipc
{
    public class PipeServer
    {
        private readonly string _pipeName;
        private CancellationTokenSource _cts;

        // Handler receives the raw request line, returns the raw response line.
        public Func<string, Task<string>>? RequestHandler { get; set; }

        public PipeServer(string pipeName)
        {
            _pipeName = pipeName;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _ = ListenLoopAsync(_cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        private async Task ListenLoopAsync(CancellationToken token)
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
                    while (pipe.IsConnected)
                    {
                        string? line = await reader.ReadLineAsync();
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
                catch (IOException)
                {
                    Debug.WriteLine("Client dropped.");
                }
            }
        }
    }
}
