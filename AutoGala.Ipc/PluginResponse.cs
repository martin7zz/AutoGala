using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Ipc
{
    public class PluginResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string ResultJson { get; set; } = "";
    }
}
