using Plugin.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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
