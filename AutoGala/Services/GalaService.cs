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

        public GalaService(IWindowService windowService)
        {
            _windowService = windowService;
        }

        public async Task HookToGalaAsync(ObservableCollection<SectionItem> sections)
        {
            var prompt = _windowService.ShowGalaPrompt();

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                AttachAndPush(clickPoint, nav => nav.WriteItems(sections));
            }
            finally
            {
                prompt.Close();
            }
        }

        public async Task HookToGalaAsync(ObservableCollection<RebarItem> rebars)
        {
            var prompt = _windowService.ShowGalaPrompt();

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                AttachAndPush(clickPoint, nav => nav.WriteItems(rebars));
            }
            finally
            {
                prompt.Close();
            }
        }

        public async Task HookToGalaAsync(ObservableCollection<LoadItem> loads)
        {
            var prompt = _windowService.ShowGalaPrompt();

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                AttachAndPush(clickPoint, nav => nav.WriteItems(loads));
            }
            finally
            {
                prompt.Close();
            }
        }

        private Task<Point> WaitForUserClickAsync()
        {
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

        private void AttachAndPush(Point screenPoint, Action<GalaNavigator> writeAction)
        {
            var clicked = AutomationElement.FromPoint(screenPoint);
            if (clicked == null)
            {
                MessageBox.Show("No element found at that point.");
                return;
            }

            var navigator = new GalaNavigator();

            if (!navigator.Attach(clicked))
            {
                MessageBox.Show("Could not locate the structure in Gala.");
                return;
            }

            writeAction(navigator);
        }
    }
}
