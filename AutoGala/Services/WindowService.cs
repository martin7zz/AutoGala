using AutoGala.Contracts;
using AutoGala.ViewModels;
using AutoGala.views;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Core.Models;
using System.Windows;

namespace AutoGala.Services
{
    public class WindowService : IWindowService
    {
        private readonly IServiceProvider _serviceProvider;

        public WindowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public EditJobInfoView ShowEditJobInfo(
            JobInfo jobInfo,
            IJobInfoChangedNotifier notifier)
        {
            var viewModel = ActivatorUtilities.CreateInstance<EditJobInfoViewModel>(
                _serviceProvider,
                jobInfo,
                notifier);

            var window = new EditJobInfoView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };

            viewModel.SaveRequested += () => window.Close();

            window.ShowDialog();

            return window;
        }


        public ClipboardErrorView ShowClipboardError(
            string data,
            IEnumerable<string>? failedRows = null)
        {
            var viewModel = ActivatorUtilities.CreateInstance<ClipboardErrorViewModel>(
                _serviceProvider,
                data,
                failedRows);

            var window = new ClipboardErrorView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };

            window.Show();

            return window;
        }


        public AutoGalaProcessSelectionView ShowProcessSelection()
        {
            var viewModel =
                _serviceProvider.GetRequiredService<AutoGalaProcessSelectionViewModel>();

            var window = new AutoGalaProcessSelectionView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
            };

            viewModel.ConnectionSucceeded += () => window.Close();

            viewModel.ConnectionFailed += message =>
                MessageBox.Show(
                    window,
                    message,
                    "Connection Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

            window.Closed += (_, _) =>
            {
                viewModel.Dispose();
            };

            window.Show();

            return window;
        }

        public GalaPromptView ShowGalaPrompt(string data)
        {
            var viewModel = ActivatorUtilities.CreateInstance<GalaPromptViewModel>(
                _serviceProvider,
                data);

            var window = new GalaPromptView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel
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
