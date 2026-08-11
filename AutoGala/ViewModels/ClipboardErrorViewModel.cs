using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using AutoGala.ViewModels.Base;

namespace AutoGala.ViewModels
{
    public class ClipboardErrorViewModel : ViewModelBase
    {
        private bool _hasFailedRows = false;

        public string Message { get; }
        public List<string> FailedRows { get; }
        public bool HasFailedRows 
        { 
            get =>  _hasFailedRows;
            set 
            {
                if (value == _hasFailedRows) return;
                _hasFailedRows = value;
                OnPropertyChanged();
            }
        }

        public ClipboardErrorViewModel(string message, IEnumerable<string>? failedRows)
        {
            Message = message;
            FailedRows = failedRows?.ToList() ?? new List<string>();
            _hasFailedRows = failedRows != null;
        }
    }
}
