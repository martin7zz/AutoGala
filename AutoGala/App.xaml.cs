using System.Configuration;
using System.Data;
using System.Windows;
using AutoGala.Contracts;
using AutoGala.Services;
using AutoGala.ViewModels;
using AutoGala.views;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Core.Contracts;
using Plugin.Core.Services;

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

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<ILoadService, LoadService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IGalaService, GalaService>();

            services.AddTransient<SectionViewModel>();
            services.AddTransient<RebarViewModel>();
            services.AddTransient<LoadViewModel>();

            services.AddTransient<SectionView>();
            services.AddTransient<RebarView>();
            services.AddTransient<LoadView>();
            services.AddTransient<GalaPromptView>();
            //services.AddTransient<WindowService>();
        }
    }
}
