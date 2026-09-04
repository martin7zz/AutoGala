using AutoGala.ViewModels.Base;

namespace AutoGala.ViewModels
{
    public class ErrorViewModel : ViewModelBase
    {
        private bool _hasFailedRows = false;

        public string Message { get; }
        public List<string> FailedRows { get; }
        public bool HasFailedRows
        {
            get => _hasFailedRows;
            set
            {
                if (value == _hasFailedRows) return;
                _hasFailedRows = value;
                OnPropertyChanged();
            }
        }

        public ErrorViewModel(string message, IEnumerable<string>? failedRows)
        {
            Message = message;
            FailedRows = failedRows?.ToList() ?? new List<string>();
            _hasFailedRows = failedRows != null;
        }
    }
}
