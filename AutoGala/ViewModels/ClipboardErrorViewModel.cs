using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AutoGala.ViewModels
{
    public class ClipboardErrorViewModel
    {
        public string Message { get; }
        public List<string> FailedRows { get; }
        public bool HasFailedRows => FailedRows.Count > 0;

        public ClipboardErrorViewModel(string message, IEnumerable<string>? failedRows)
        {
            Message = message;
            FailedRows = failedRows?.ToList() ?? new List<string>();
        }
    }
}
