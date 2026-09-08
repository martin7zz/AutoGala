using AutoGala.Common;
using AutoGala.ViewModels.Base;
using System.Globalization;
using System.Windows.Input;

namespace AutoGala.ViewModels
{
    public class ScaleFactorViewModel : ViewModelBase
    {
        private string _scaleFactorText;
        private string? _errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public string ScaleFactorText
        {
            get => _scaleFactorText;
            set
            {
                if (_scaleFactorText == value)
                    return;

                _scaleFactorText = value;
                OnPropertyChanged();
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage == value)
                    return;

                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<double>? Confirmed;
        public event Action? Cancelled;

        public ScaleFactorViewModel(double currentScaleFactor)
        {
            _scaleFactorText = currentScaleFactor.ToString(CultureInfo.InvariantCulture);

            OkCommand = new RelayCommand(param => Ok());
            CancelCommand = new RelayCommand(param => Cancelled?.Invoke());
        }

        private void Ok()
        {
            if (double.TryParse(
                    ScaleFactorText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var scaleFactor)
                && scaleFactor > 0)
            {
                ErrorMessage = null;
                Confirmed?.Invoke(scaleFactor);
            }
            else
            {
                ErrorMessage = "Enter a positive number (e.g. 0.1)";
            }
        }
    }
}