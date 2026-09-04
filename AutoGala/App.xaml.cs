using AutoGala.Contracts;
using AutoGala.Services;
using AutoGala.Services.Notifiers;
using AutoGala.ViewModels;
using AutoGala.views;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using Plugin.Core.Services;
using System.Windows;

namespace AutoGala
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _servicesProvider;

        public App()
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            _servicesProvider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _servicesProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var pipeClient =
                    _servicesProvider.GetRequiredService<
                        IAutoGalaPipeClientService>();

                pipeClient.Disconnect();
            }
            finally
            {
                base.OnExit(e);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<JobInfo>();
            services.AddSingleton<IJobInfoChangedNotifier, JobInfoChangedNotifier>();

            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<ILoadService, LoadService>();
            services.AddSingleton<IRebarService, RebarService>();
            services.AddSingleton<ISectionService, SectionService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IGalaService, GalaService>();
            services.AddSingleton<IMainWindowService, MainWindowService>();
            services.AddSingleton<IAutoGalaProcessService, AutoGalaProcessService>();
            services.AddSingleton<IAutoGalaPipeClientService, AutoGalaPipeClientService>();
            services.AddSingleton<IMessageExchangeService, MessageExchangeService>();
            services.AddSingleton<IAutoCADOperationRunner, AutoCADOperationRunner>();

            services.AddTransient<SectionViewModel>();
            services.AddTransient<RebarViewModel>();
            services.AddTransient<LoadViewModel>();
            services.AddTransient<ErrorViewModel>();
            services.AddTransient<GalaPromptViewModel>();
            services.AddTransient<EditJobInfoViewModel>();
            services.AddTransient<AutoGalaProcessSelectionViewModel>();

            services.AddTransient<SectionView>();
            services.AddTransient<RebarView>();
            services.AddTransient<LoadView>();
            services.AddTransient<GalaPromptView>();
            services.AddTransient<ErrorView>();
            services.AddTransient<EditJobInfoView>();
            services.AddTransient<AutoGalaProcessSelectionView>();
        }
    }
}
