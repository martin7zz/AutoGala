using AutoGala.Contracts;
using AutoGala.ViewModels;
using AutoGala.views;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;

namespace AutoGala.Services
{
    public class WindowService : IWindowService
    {
        public EditJobInfoView ShowEditJobInfo(JobInfo jobInfo, IJobInfoChangedNotifier notifier)
        {
            var viewModel = new EditJobInfoViewModel(jobInfo, notifier);
            
            var window = new EditJobInfoView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };

            viewModel.SaveRequested += () => window.Close();
            window.ShowDialog();

            return window;
        }

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

        public AutoGalaProcessSelectionView ShowProcessSelection(IAutoGalaProcessService autoGalaProcessService, IAutoGalaPipeClientService autoGalaPipeClientService)
        {
            var viewModel = new AutoGalaProcessSelectionViewModel(autoGalaProcessService, autoGalaPipeClientService);

            var window = new AutoGalaProcessSelectionView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };

            viewModel.ConnectionSucceeded += () => window.Close();
            viewModel.ConnectionFailed += message =>
                MessageBox.Show(window, message, "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);

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
