using AutoGala.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.ViewModels
{
    public class LoadingWindowViewModel : ViewModelBase
    {
        private string _message;

        public string Message
        {
            get => _message;
            set
            {
                if (value == _message) return;

                _message = value;
                OnPropertyChanged();
            }
        }

        public LoadingWindowViewModel(string message)
        {
            _message = message;
        }
    }
}
