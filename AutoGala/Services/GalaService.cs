using AutoGala.Contracts;
using AutoGala.Services.Helper;
using Gma.System.MouseKeyHook;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;
using static AutoGala.Common.NotificationMessages;
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

        private async Task HookToGalaAsync<T>(
            ObservableCollection<T> items,
            Func<GalaNavigator, ObservableCollection<T>, bool> write)
        {
            if (items.Count == 0)
            {
                MessageBox.Show(NoDataErrorMessage);
                return;
            }

            var prompt = _windowService.ShowGalaPrompt(WaitingGalaClickMessage);
            string? errorMessage = null;

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                _windowService.UpdateGalaPrompt(TransferingToGalaMessage, prompt);

                errorMessage = await Task.Run(() => AttachAndPush(clickPoint, nav => write(nav, items)));
            }
            finally
            {
                prompt.Close();
            }

            if (errorMessage != null)
            {
                MessageBox.Show(errorMessage);
            }
        }

        public Task HookToGalaAsync(ObservableCollection<SectionItem> items) =>
            HookToGalaAsync(items, (nav, i) => nav.WriteItems(i));

        public Task HookToGalaAsync(ObservableCollection<RebarItem> items) =>
            HookToGalaAsync(items, (nav, i) => nav.WriteItems(i));

        public Task HookToGalaAsync(ObservableCollection<LoadItem> items) =>
            HookToGalaAsync(items, (nav, i) => nav.WriteItems(i));

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

        private string? AttachAndPush(Point screenPoint, Func<GalaNavigator, bool> writeAction)
        {
            try
            {
                var clicked = AutomationElement.FromPoint(screenPoint);
                if (clicked == null)
                    return NoGalaElementFoundErrorMessage;

                var navigator = new GalaNavigator();

                if (!navigator.Attach(clicked))
                    return NoGalaStructureFoundErrorMessage;

                if (!writeAction(navigator))
                    return GalaError;
                return null;
            }

            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return $"Unexpected error while writing to Gala: {ex.Message}";
            }
        }
    }
}
