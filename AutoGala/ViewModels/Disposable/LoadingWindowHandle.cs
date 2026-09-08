using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AutoGala.ViewModels.Disposable
{
    public sealed class LoadingWindowHandle : IDisposable
    {
        private readonly Window _window;

        public LoadingWindowHandle(Window window)
        {
            _window = window;
        }

        public string Message
        {
            get => ((LoadingWindowViewModel)_window.DataContext).Message;
            set => ((LoadingWindowViewModel)_window.DataContext).Message = value;
        }

        public void Dispose()
        {
            _window.Close();
        }
    }
}
