using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.Services;
using AutoGala.ViewModels.Base;
using DocumentFormat.OpenXml.Spreadsheet;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AutoGala.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public SectionViewModel SectionView { get; }
        public RebarViewModel RebarView { get; }
        public LoadViewModel LoadView { get; }
        public EditJobInfoViewModel EditJobInfoView { get; }
        public AutoGalaProcessSelectionViewModel AutoGalaProcessSelectionView { get; }

        private IDialogService _dialogService;
        private IMainWindowService _mainWindowService;
        private IWindowService _windowService;
        private IGalaService _galaService;
        private IAutoGalaPipeClientService _autoGalaPipeClientService;
        private IMessageExchangeService _messageExchageService;
        private IJobInfoChangedNotifier _notifier;
        private IAutoCADOperationRunner _autoCADRunnerService;
        private IAutoCADSettingsService _autoCADSettingsService;
        private readonly IAutoCADStateService _autoCADStateService;

        public ICommand SaveAllToExcelCommand { get; }
        public ICommand LoadAllFromExcelCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand EditJobInfoCommand { get; }
        public ICommand SetJobInfoCommand { get; }
        public ICommand ConnectToAutoCADCommand { get; }
        public ICommand GetAllFromAutoCADCommand { get; }
        public ICommand ScaleFactorCommand { get; }

        public MainWindowViewModel(SectionViewModel sectionViewModel,
            RebarViewModel rebarViewModel,
            LoadViewModel loadViewModel,
            EditJobInfoViewModel editJobInfoViewModel,
            AutoGalaProcessSelectionViewModel autoGalaProcessSelectionViewModel,
            IDialogService dialogService,
            IMainWindowService mainWindowService,
            IWindowService windowService,
            IGalaService galaService,
            IAutoGalaPipeClientService autoGalaPipeClientService,
            IJobInfoChangedNotifier notifier,
            IMessageExchangeService messageExchangeService,
            IAutoCADOperationRunner autoCADOperationRunnerService,
            IAutoCADSettingsService autoCADSettingsService,
            IAutoCADStateService autoCADStateService)
        {
            SectionView = sectionViewModel;
            RebarView = rebarViewModel;
            LoadView = loadViewModel;
            EditJobInfoView = editJobInfoViewModel;
            AutoGalaProcessSelectionView = autoGalaProcessSelectionViewModel;

            _dialogService = dialogService;
            _mainWindowService = mainWindowService;
            _windowService = windowService;
            _galaService = galaService;
            _autoGalaPipeClientService = autoGalaPipeClientService;
            _notifier = notifier;
            _messageExchageService = messageExchangeService;
            _autoCADRunnerService = autoCADOperationRunnerService;
            _autoCADSettingsService = autoCADSettingsService;
            _autoCADStateService = autoCADStateService;

            _autoGalaPipeClientService.ConnectionStateChanged += () => CommandManager.InvalidateRequerySuggested();
            _autoCADStateService.StateChanged += () => CommandManager.InvalidateRequerySuggested();

            SaveAllToExcelCommand = new RelayCommand(param => SaveAllToExcel(), param => HasAll());
            LoadAllFromExcelCommand = new RelayCommand(param => LoadAllFromExcel());
            ClearAllCommand = new RelayCommand(param => ClearAll(), param => HasData());
            EditJobInfoCommand = new RelayCommand(param => EditJobInfo());
            SetJobInfoCommand = new RelayCommand(async param => await SetJobInfoAsync());
            ConnectToAutoCADCommand = new RelayCommand(async param => await ConnectToAutoCADAsync(), param => !_autoGalaPipeClientService.IsConnected);
            GetAllFromAutoCADCommand = new RelayCommand(async param => await GetAllFromAutoCADAsync(),
                param => _autoGalaPipeClientService.IsConnected && _autoCADStateService.HasActiveDocument);
            ScaleFactorCommand = new RelayCommand(param => ChangeScaleFactor());
        }

        private void ChangeScaleFactor()
        {
            var newScaleFactor = _windowService.ShowScaleFactor(_autoCADSettingsService.ScaleFactor);

            if (newScaleFactor.HasValue)
            {
                _autoCADSettingsService.ScaleFactor = newScaleFactor.Value;
            }
        }

        public bool ConfirmClose()
        {
            return _dialogService.Confirm(
                "Are you sure you want to close?",
                "Exit"
            );
        }

        private void EditJobInfo()
        {
            _windowService.ShowEditJobInfo(EditJobInfoView.JobInfo, _notifier);
            EditJobInfoView.RefreshFromModel();
        }

        private async Task SetJobInfoAsync()
        {
            try
            {
                await _galaService.HookToGalaJobAsync(EditJobInfoView.JobInfo);
            }
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError(ex.Message);
                return;
            }
        }

        private async Task ConnectToAutoCADAsync()
        {
            _windowService.ShowProcessSelection();
        }

        private async Task GetAllFromAutoCADAsync()
        {
            var (success, shapeData) = await _autoCADRunnerService.RunAsync(() =>
            _messageExchageService.GetAllAsync(_autoGalaPipeClientService, EditJobInfoView.JobInfo.JobTitle));

            if (!success)
            {
                return;
            }

            if (shapeData.Item1.Item1.Any() || shapeData.Item1.Item2.Any())
            {
                ClearAllAutoCAD();
            }

            foreach (var section in shapeData.Item1.Item1)
            {
                section.X *= _autoCADSettingsService.ScaleFactor;
                section.Y *= _autoCADSettingsService.ScaleFactor;
                SectionView.Sections.Add(section);
            }

            foreach (var rebar in shapeData.Item1.Item2)
            {
                rebar.Area *= Math.Pow(_autoCADSettingsService.ScaleFactor, 2);
                rebar.X *= _autoCADSettingsService.ScaleFactor;
                rebar.Y *= _autoCADSettingsService.ScaleFactor;
                RebarView.Rebars.Add(rebar);
            }

            EditJobInfoView.JobInfo.JobTitle = shapeData.Item2;
            _notifier.NotifyJobInfoChanged();

            CommandManager.InvalidateRequerySuggested();
        }

        private void SaveAllToExcel()
        {
            try
            {
                _mainWindowService.SaveAllToExcel(
                    SectionView.Sections,
                    RebarView.Rebars,
                    LoadView.Loads,
                    LoadView.IsSimpleBending,
                    EditJobInfoView.JobInfo);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _windowService.ShowError($"File was not saved: {ex.Message}");
                return;
            }
        }

        private void LoadAllFromExcel()
        {
            List<object> items;
            try
            {
                items = _mainWindowService.LoadAllExcel(LoadView.IsSimpleBending);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _windowService.ShowError($"Unable to load file: {ex.Message}");
                return;
            }

            if (items == null || items.Count < 3)
            {
                return;
            }

            ClearAll();

            foreach (var section in (ObservableCollection<SectionItem>)items[0])
            {
                SectionView.Sections.Add(section);
            }
            foreach (var rebar in (ObservableCollection<RebarItem>)items[1])
            {
                RebarView.Rebars.Add(rebar);
            }
            foreach (var load in (ObservableCollection<LoadItem>)items[2])
            {
                LoadView.Loads.Add(load);
            }

            var loadedJobInfo = (JobInfo)items[3];
            var jobInfo = EditJobInfoView.JobInfo;

            jobInfo.JobTitle = loadedJobInfo.JobTitle;
            jobInfo.JobNumber = loadedJobInfo.JobNumber;
            jobInfo.Client = loadedJobInfo.Client;
            jobInfo.CalcsBy = loadedJobInfo.CalcsBy;
            jobInfo.CheckedBy = loadedJobInfo.CheckedBy;

            EditJobInfoView.RefreshFromModel();

            CommandManager.InvalidateRequerySuggested();
        }

        private void ClearAllAutoCAD()
        {
            SectionView.ClearList();
            RebarView.ClearList();
        }

        private void ClearAll()
        {
            SectionView.ClearList();
            RebarView.ClearList();
            LoadView.ClearList();
        }

        private bool HasData()
        {
            return SectionView.Sections.Count > 0 ||
                RebarView.Rebars.Count > 0 ||
                LoadView.Loads.Count > 0;
        }

        private bool HasAll()
        {
            return SectionView.Sections.Count > 0 &&
                RebarView.Rebars.Count > 0 &&
                LoadView.Loads.Count > 0;
        }
    }
}
