using AutoGala.Contracts;
using AutoGala.Services.Helper;
using Gma.System.MouseKeyHook;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;
using static AutoGala.Common.NotificationMessages;

namespace AutoGala.Services
{
    public class GalaService : IGalaService
    {

        private readonly IWindowService _windowService;
        private readonly ISectionService _sectionService;
        private readonly IRebarService _rebarService;
        private readonly ILoadService _loadService;

        public GalaService(IWindowService windowService, ISectionService sectionService, IRebarService rebarService, ILoadService loadService)
        {
            _windowService = windowService;
            _sectionService = sectionService;
            _rebarService = rebarService;
            _loadService = loadService;
        }

        private async Task HookToGalaJobAsync(
            JobInfo jobInfo,
            Func<JobNavigator, JobInfo, bool> write)
        {
            if (jobInfo == null)
            {
                throw new InvalidOperationException(JobInfoNullErrorMessage);
            }

            var prompt = _windowService.ShowGalaPrompt(
                WaitingGalaJobInfoClickMessage);

            string? errorMessage = null;

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                _windowService.UpdateGalaPrompt(
                    TransferingToGalaMessage,
                    prompt);

                errorMessage = await Task.Run(() =>
                    AttachAndPushJob(
                        clickPoint,
                        nav => write(nav, jobInfo)));
            }
            finally
            {
                prompt.Close();
            }

            if (errorMessage != null)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        public Task HookToGalaJobAsync(JobInfo jobInfo) =>
            HookToGalaJobAsync(
        jobInfo,
        (nav, job) => nav.WriteJob(job));

        private async Task HookToGalaAsync<T>(
            ObservableCollection<T> items,
            Func<GalaNavigator, ObservableCollection<T>, bool> write)
        {
            if (items.Count == 0)
            {
                throw new InvalidOperationException(NoDataErrorMessage);
            }

            var prompt = _windowService.ShowGalaPrompt(WaitingGalaClickMessage);
            string? errorMessage = null;

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                _windowService.UpdateGalaPrompt(TransferingToGalaMessage, prompt);

                errorMessage = await Task.Run(() => AttachAndPushGeneral(clickPoint, nav => write(nav, items)));
            }
            finally
            {
                prompt.Close();
            }

            if (errorMessage != null)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        public Task HookToGalaAsync(ObservableCollection<SectionItem> items) =>
            HookToGalaAsync(items, (nav, i) => nav.WriteItems(i));

        public Task HookToGalaAsync(ObservableCollection<RebarItem> items) =>
            HookToGalaAsync(items, (nav, i) => nav.WriteItems(i));

        public Task HookToGalaAsync(ObservableCollection<LoadItem> items, bool isSimpleBending) =>
            HookToGalaAsync(items, (nav, i) => nav.WriteItems(i, isSimpleBending));

        private async Task<ObservableCollection<T>> GetFromGalaAsync<T>(Func<GalaNavigator, ObservableCollection<T>?> readItems)
        {
            var prompt = _windowService.ShowGalaPrompt(WaitingGalaClickMessage);
            string? errorMessage = null;
            ObservableCollection<T>? items;

            try
            {
                var clickPoint = await WaitForUserClickAsync();

                _windowService.UpdateGalaPrompt(TransferingFromGalaMessage, prompt);

                (errorMessage, items) = await Task.Run(() => AttachAndPullGeneral(clickPoint, readItems));
            }
            finally
            {
                prompt.Close();
            }

            if (errorMessage != null || items == null)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return items;
        }

        public Task<ObservableCollection<SectionItem>> GetSectionsFromGalaAsync() =>
            GetFromGalaAsync<SectionItem>(nav => nav.ReadItems(_sectionService, out ObservableCollection<SectionItem> sections) ? sections : null);

        public Task<ObservableCollection<RebarItem>> GetRebarsFromGalaAsync() =>
            GetFromGalaAsync<RebarItem>(nav => nav.ReadItems(_rebarService, out ObservableCollection<RebarItem> rebars) ? rebars : null);

        public Task<ObservableCollection<LoadItem>> GetLoadsFromGalaAsync(bool isSimpleBending) =>
            GetFromGalaAsync<LoadItem>(nav => nav.ReadItems(_loadService, isSimpleBending, out ObservableCollection<LoadItem> loads) ? loads : null);

        private string? AttachToGala<TNavigator>(
            Point screenPoint,
            Func<TNavigator, bool> galaAction)
            where TNavigator : NavigatorBase, new()
        {
            try
            {
                var clicked = AutomationElement.FromPoint(screenPoint);

                if (clicked == null)
                    return NoGalaElementFoundErrorMessage;

                var navigator = new TNavigator();

                if (!navigator.Attach(clicked))
                    return NoGalaStructureFoundErrorMessage;

                if (!galaAction(navigator))
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

        private string? AttachAndPushJob(
            Point screenPoint,
            Func<JobNavigator, bool> writeAction)
        {
            return AttachToGala(screenPoint, writeAction);
        }

        private string? AttachAndPushGeneral(
            Point screenPoint,
            Func<GalaNavigator, bool> writeAction)
        {
            return AttachToGala(screenPoint, writeAction);
        }

        private (string? Error, ObservableCollection<T>? Items) AttachAndPullGeneral<T>(
            Point screenPoint,
            Func<GalaNavigator, ObservableCollection<T>?> readAction)
        {
            try
            {
                var clicked = AutomationElement.FromPoint(screenPoint);

                if (clicked == null)
                    return (NoGalaElementFoundErrorMessage, null);

                var navigator = new GalaNavigator();

                if (!navigator.Attach(clicked))
                    return (NoGalaStructureFoundErrorMessage, null);

                var items = readAction(navigator);

                return items == null ? (GalaError, null) : (null, items);
            }
            catch (InvalidOperationException ex)
            {
                return (ex.Message, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return ($"Unexpected error while reading from Gala: {ex.Message}", null);
            }
        }

        private string? AttachAndPullGeneral(
            Point screenPoint,
            Func<GalaNavigator, bool> readAction)
        {
            return AttachAndPullGeneral(screenPoint, readAction);
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
    }
}
