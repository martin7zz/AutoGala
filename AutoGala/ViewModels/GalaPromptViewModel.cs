using AutoGala.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.ViewModels
{
    public class GalaPromptViewModel : ViewModelBase
    {
        private string _message;
        private bool _isWaiting = true;

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

        public bool IsWaiting
        {
            get => _isWaiting;
            set
            {
                if (value == _isWaiting) return;
                _isWaiting = value;
                OnPropertyChanged();
            }
        }

        public GalaPromptViewModel(string message)
        {
            _message = message;
        }
    }
}
