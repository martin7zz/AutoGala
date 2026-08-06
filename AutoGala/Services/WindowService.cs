using AutoGala.Contracts;
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
        public GalaPromptView ShowGalaPrompt()
        {
            var window = new GalaPromptView
            {
                Owner = Application.Current.MainWindow
            };

            window.Show();

            return window;
        }
    }
}
