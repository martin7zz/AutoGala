using System.Diagnostics;

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
