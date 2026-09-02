using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace AutoGala.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public SectionViewModel SectionView { get; }
        public RebarViewModel RebarView { get; }
        public LoadViewModel LoadView { get; }
        public EditJobInfoViewModel EditJobInfoView { get; }
        public AutoGalaProcessSelectionViewModel AutoGalaProcessSelectionView { get; }


        private IMainWindowService _mainWindowService;
        private IWindowService _windowService;
        private IGalaService _galaService;
        private IAutoGalaProcessService _autoGalaProcessService;
        private IAutoGalaPipeClientService _autoGalaPipeClientService;
        private IMessageExchangeService _messageExchageService;
        private IJobInfoChangedNotifier _notifier;

        public ICommand SaveAllToExcelCommand { get; }
        public ICommand LoadAllFromExcelCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand EditJobInfoCommand { get; }
        public ICommand SetJobInfoCommand { get; }
        public ICommand ConnectToAutoCADCommand { get; }
        public ICommand GetAllFromAutoCADCommand { get; }

        public MainWindowViewModel(SectionViewModel sectionViewModel,
            RebarViewModel rebarViewModel,
            LoadViewModel loadViewModel,
            EditJobInfoViewModel editJobInfoViewModel,
            AutoGalaProcessSelectionViewModel autoGalaProcessSelectionViewModel,
            IMainWindowService mainWindowService,
            IWindowService windowService,
            IGalaService galaService,
            IAutoGalaProcessService autoGalaProcessService,
            IAutoGalaPipeClientService autoGalaPipeClientService,
            IJobInfoChangedNotifier notifier,
            IMessageExchangeService messageExchangeService)
        {
            SectionView = sectionViewModel;
            RebarView = rebarViewModel;
            LoadView = loadViewModel;
            EditJobInfoView = editJobInfoViewModel;
            AutoGalaProcessSelectionView = autoGalaProcessSelectionViewModel;

            _mainWindowService = mainWindowService;
            _windowService = windowService;
            _galaService = galaService;
            _autoGalaProcessService = autoGalaProcessService;
            _autoGalaPipeClientService = autoGalaPipeClientService;
            _notifier = notifier;
            _messageExchageService = messageExchangeService;

            _autoGalaPipeClientService.ConnectionStateChanged += () => CommandManager.InvalidateRequerySuggested();

            SaveAllToExcelCommand = new RelayCommand(param => SaveAllToExcel(), param => HasAll());
            LoadAllFromExcelCommand = new RelayCommand(param => LoadAllFromExcel());
            ClearAllCommand = new RelayCommand(param => ClearAll(), param => HasData());
            EditJobInfoCommand = new RelayCommand(param => EditJobInfo());
            SetJobInfoCommand = new RelayCommand(async param => await SetJobInfoAsync());
            ConnectToAutoCADCommand = new RelayCommand(async param => await ConnectToAutoCADAsync(), param => !_autoGalaPipeClientService.IsConnected);
            GetAllFromAutoCADCommand = new RelayCommand(async param => await GetAllFromAutoCADAsync(), param => _autoGalaPipeClientService.IsConnected);
        }

        private void EditJobInfo()
        {
            _windowService.ShowEditJobInfo(EditJobInfoView.JobInfo, _notifier);
            EditJobInfoView.RefreshFromModel();
        }

        private async Task SetJobInfoAsync()
        {
            await _galaService.HookToGalaJobAsync(EditJobInfoView.JobInfo);
        }

        private async Task ConnectToAutoCADAsync()
        {
            _windowService.ShowProcessSelection();
        }

        private async Task GetAllFromAutoCADAsync()
        {
            try
            {
                _autoGalaPipeClientService.ActivateAutoCAD();

                var shapeData = await _messageExchageService.GetAllAsync(_autoGalaPipeClientService, EditJobInfoView.JobInfo.JobTitle);

                if (shapeData.Item1.Item1.Any() || shapeData.Item1.Item2.Any())
                {
                    ClearAll();
                }

                foreach (var section in shapeData.Item1.Item1)
                {
                    SectionView.Sections.Add(section);
                }

                foreach (var rebar in shapeData.Item1.Item2)
                {
                    RebarView.Rebars.Add(rebar);
                }

                EditJobInfoView.JobInfo.JobTitle = shapeData.Item2;
                _notifier.NotifyJobInfoChanged();

                CommandManager.InvalidateRequerySuggested();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "AutoGala", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveAllToExcel()
        {
            _mainWindowService.SaveAllToExcel(
                SectionView.Sections,
                RebarView.Rebars,
                LoadView.Loads,
                LoadView.IsSimpleBending,
                EditJobInfoView.JobInfo);
        }

        private void LoadAllFromExcel()
        {

            var items = _mainWindowService.LoadAllExcel(LoadView.IsSimpleBending);

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
