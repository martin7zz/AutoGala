using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Core.Models
{
    public class AutoCADApplication
    {
        public int ProcessId { get; init; }
        public string ProcessName { get; init; } = "";
        public string WindowTitle { get; init; } = "";
        public Process? Process { get; init; }
    }
}
