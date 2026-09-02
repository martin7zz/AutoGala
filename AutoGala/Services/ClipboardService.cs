using Plugin.Core.Contracts;
using System.Windows;

namespace AutoGala.Services
{
    public class ClipboardService : IClipboardService
    {
        public string GetText()
        {
            return Clipboard.GetText();
        }
    }
}
