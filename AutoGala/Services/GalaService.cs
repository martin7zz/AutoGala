using AutoGala.Contracts;
using AutoGala.Services.Helper;
using Gma.System.MouseKeyHook;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace AutoGala.Services
{
    public class GalaService : IGalaService
    {

        private readonly IWindowService _windowService;
        private AutomationElement? _rootPane;
        private AutomationElement? _casesEdit;

        public GalaService(IWindowService windowService)
        {
            _windowService = windowService;
        }

        public async Task HookToGalaAsync(ObservableCollection<LoadItem> loads)
        {
            var prompt = _windowService.ShowGalaPrompt();

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                AttachAndPush(clickPoint, loads);
            }
            finally
            {
                prompt.Close();
            }
        }

        private Task<Point> WaitForUserClickAsync()
        {
            // Temporary implementation.
            // Replace this with your mouse hook later.

            var tcs = new TaskCompletionSource<Point>();

            var hook = Hook.GlobalEvents();

            var dispatcher = System.Windows.Application.Current.Dispatcher;

            void OnMouseDown(object? sender, MouseEventArgs e)
            {
                hook.MouseDown -= OnMouseDown;
                hook.Dispose();

                var point = new Point(e.X, e.Y);

                dispatcher.Invoke(() => tcs.TrySetResult(point));
            }

            hook.MouseDown += OnMouseDown;

            return tcs.Task;
        }

        private void AttachAndPush(Point screenPoint, ObservableCollection<LoadItem> loads)
        {
            var clicked = AutomationElement.FromPoint(screenPoint);
            Console.WriteLine(clicked.Current.Name);
            if (clicked == null)
            {
                MessageBox.Show("No element found at that point.");
                return;
            }

            var loadsNav = new LoadsGalaNavigator();
            if (!loadsNav.Attach(clicked))
            {
                MessageBox.Show("Could not locate the Loads structure in Gala.");
                return;
            }

            if (!loadsNav.WriteLoads(loads))
            {
                MessageBox.Show("Failed to write all rows — row count or tab index may be off.");
            }
        }
    }
}
