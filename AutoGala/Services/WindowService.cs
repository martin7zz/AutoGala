using AutoGala.Contracts;
using AutoGala.ViewModels;
using AutoGala.views;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;

namespace AutoGala.Services
{
    public class WindowService : IWindowService
    {
        public ClipboardErrorView ShowClipboardError(string data, IEnumerable<string>? failedRows = null)
        {
            var window = new ClipboardErrorView
            {
                Owner = Application.Current.MainWindow,
                DataContext = new ClipboardErrorViewModel(data, failedRows)
            };

            window.Show();

            return window;
        }

        public GalaPromptView ShowGalaPrompt(string data)
        {
            var window = new GalaPromptView
            {
                Owner = Application.Current.MainWindow,
                DataContext = new GalaPromptViewModel(data)
            };

            window.Show();

            return window;
        }

        public void UpdateGalaPrompt(string data, GalaPromptView galaPromptView)
        {
            if (galaPromptView.DataContext is GalaPromptViewModel vm)
            {
                vm.Message = data;
                vm.IsWaiting = false;
            }
        }
    }
}
