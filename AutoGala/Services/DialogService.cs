using AutoGala.Contracts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AutoGala.Services
{
    public class DialogService : IDialogService
    {
        public bool Confirm(string message, string title)
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            ) == MessageBoxResult.Yes;
        }
    }
}
