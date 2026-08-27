using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Ipc
{
    public class PluginRequest
    {
        public string Action { get; set; } = "";
        public string PayloadJson { get; set; } = "";
    }
}
