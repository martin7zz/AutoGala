using AutoGala.Ipc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AutoGala.Contracts
{
    public interface IAutoGalaPipeClientService : IAsyncDisposable
    {
        bool IsConnected { get; }
        Task ConnectAsync(Process process, CancellationToken ct = default);
        event Action? ConnectionStateChanged;
        Task<PluginResponse> SendAsync(PluginRequest request, CancellationToken ct = default);
        void Disconnect();
    }
}
