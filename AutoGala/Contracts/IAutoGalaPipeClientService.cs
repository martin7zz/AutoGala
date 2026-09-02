using AutoGala.Ipc;
using System.Diagnostics;

namespace AutoGala.Contracts
{
    public interface IAutoGalaPipeClientService : IAsyncDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(Process process, CancellationToken ct = default);
        event Action? ConnectionStateChanged;
        void ActivateAutoCAD();
        Task<PluginResponse> SendAsync(PluginRequest request, CancellationToken ct = default);
        void Disconnect();
    }
}
