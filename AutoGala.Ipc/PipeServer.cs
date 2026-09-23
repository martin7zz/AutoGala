using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace AutoGala.Ipc
{
    /// <summary>
    /// Line-based named pipe server: one JSON request per line in, one JSON response per line out.
    /// Serves one client at a time and goes back to listening when the client disconnects.
    /// </summary>
    public class PipeServer
    {
        private const int BufferSize = 1024;
        private const int PipeBufferSize = 65536;
        private const int ErrorRetryDelayMs = 200;

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _pipeName;
        private CancellationTokenSource? _cts;
        private Task? _listenTask;
        private int _connectionSequence;

        /// <summary>Receives the raw request line, returns the raw response line.</summary>
        public Func<string, Task<string>>? RequestHandler { get; set; }

        /// <summary>Called from background threads, so it must be thread-safe.</summary>
        public Action<string>? Log { get; set; }

        public PipeServer(string pipeName)
        {
            _pipeName = pipeName;
        }

        public void Start()
        {
            if (_listenTask != null)
            {
                Log?.Invoke($"PipeServer.Start: already started, ignoring (pipeName={_pipeName})");
                return;
            }

            Log?.Invoke($"PipeServer.Start: pipeName={_pipeName}");

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // Task.Run keeps the whole loop off the caller's thread. In AutoCAD that is the
            // main thread, and awaits inside the loop would otherwise resume on it.
            _listenTask = Task.Run(() => ListenLoopAsync(token));
        }

        public async Task StopAsync()
        {
            Log?.Invoke("PipeServer.StopAsync: ENTER");

            var cts = _cts;
            var listenTask = _listenTask;

            if (cts is null)
            {
                Log?.Invoke("PipeServer.StopAsync: _cts already null, nothing to stop");
                return;
            }

            _cts = null;
            _listenTask = null;

            cts.Cancel();

            try
            {
                if (listenTask != null)
                    await listenTask;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                cts.Dispose();
            }

            Log?.Invoke("PipeServer.StopAsync: EXIT");
        }

        private async Task ListenLoopAsync(CancellationToken token)
        {
            Log?.Invoke("ListenLoopAsync: starting listen loop");
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int connId = ++_connectionSequence;
                    using var pipe = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous,
                        inBufferSize: PipeBufferSize,
                        outBufferSize: PipeBufferSize);

                    Log?.Invoke($"[conn {connId}] waiting for connection...");

                    try
                    {
                        await pipe.WaitForConnectionAsync(token);
                        Log?.Invoke($"[conn {connId}] client connected");
                        await ServeClientAsync(pipe, connId, token);
                        Log?.Invoke($"[conn {connId}] ServeClientAsync returned, pipe.IsConnected={pipe.IsConnected}");
                    }
                    catch (OperationCanceledException)
                    {
                        Log?.Invoke($"[conn {connId}] canceled while waiting/serving");
                    }
                    catch (Exception) when (token.IsCancellationRequested)
                    {
                        // On the .NET Framework build, cancelling closes the pipe under a pending read.
                        Log?.Invoke($"[conn {connId}] exception during cancellation (expected on .NET Framework)");
                    }
                    catch (Exception ex)
                    {
                        Log?.Invoke($"[conn {connId}] Pipe connection error: {ex}");
                        await Task.Delay(ErrorRetryDelayMs); // avoid a hot loop
                    }
                }

                Log?.Invoke("ListenLoopAsync: loop exited (cancellation requested)");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Pipe server loop died: {ex}");
                throw;
            }
        }

        private async Task ServeClientAsync(NamedPipeServerStream pipe, int connId, CancellationToken token)
        {
            // Both wrappers use leaveOpen and own nothing: the caller disposes the pipe.
            // Not disposing them avoids a flush into a pipe the client may already have closed.
            var reader = new StreamReader(pipe, Utf8NoBom, detectEncodingFromByteOrderMarks: true, BufferSize, leaveOpen: true);
            var writer = new StreamWriter(pipe, Utf8NoBom, BufferSize, leaveOpen: true) { AutoFlush = true };

#if !NET7_0_OR_GREATER
            // StreamReader.ReadLineAsync has no CancellationToken overload before .NET 7.
            // Closing the pipe on cancel makes the blocked read throw.
            using var registration = token.Register(() =>
            {
                Log?.Invoke($"[conn {connId}] cancellation requested, closing pipe under pending read");
                try { pipe.Close(); } catch { /* already closing */ }
            });
#endif

            int lineCount = 0;
            while (pipe.IsConnected && !token.IsCancellationRequested)
            {
#if NET7_0_OR_GREATER
                string? line = await reader.ReadLineAsync(token);
#else
                string? line = await reader.ReadLineAsync();
#endif
                if (line is null)
                {
                    Log?.Invoke($"[conn {connId}] ReadLineAsync returned null -> client disconnected (linesServed={lineCount})");
                    break; // client disconnected
                }

                lineCount++;
                Log?.Invoke($"[conn {connId}] line {lineCount} received ({line.Length} chars)");

                string response = await HandleLineAsync(line);

                Log?.Invoke($"[conn {connId}] ABOUT TO WRITE RESPONSE ({response.Length} chars)");

                try
                {
                    await writer.WriteLineAsync(response);
                }
                catch (Exception ex)
                {
                    // Writing back to a pipe the client already closed. Logged explicitly so it's
                    // never confused with a read-side disconnect (line is null, above).
                    Log?.Invoke($"[conn {connId}] WriteLineAsync failed: {ex}");
                    throw;
                }

                Log?.Invoke($"[conn {connId}] RESPONSE WRITE COMPLETE");
            }

            Log?.Invoke($"[conn {connId}] ServeClientAsync exiting loop: pipe.IsConnected={pipe.IsConnected}, tokenCancelled={token.IsCancellationRequested}");
        }

        private async Task<string> HandleLineAsync(string line)
        {
            if (RequestHandler is null)
            {
                Log?.Invoke("HandleLineAsync: no RequestHandler registered");
                return ErrorResponse("No handler registered");
            }

            try
            {
                return await RequestHandler(line);
            }
            catch (Exception ex)
            {
                // Keep the connection alive: one bad request shouldn't drop the client.
                Log?.Invoke($"Request handler failed: {ex}");
                return ErrorResponse(ex.Message);
            }
        }

        private static string ErrorResponse(string error) =>
            JsonSerializer.Serialize(new PluginResponse { Success = false, Error = error });
    }
}