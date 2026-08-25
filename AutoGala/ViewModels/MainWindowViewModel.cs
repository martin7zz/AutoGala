using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using AutoGala.views;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace AutoGala.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public SectionViewModel SectionView { get; }
        public RebarViewModel RebarView { get; }
        public LoadViewModel LoadView { get; }
        public EditJobInfoViewModel EditJobInfoView { get; }


        private IMainWindowService _mainWindowService;
        private IEditStateService _editStateService;
        private IWindowService _windowService;
        private IGalaService _galaService;
        private IJobInfoChangedNotifier _notifier;

        public ICommand SaveAllToExcelCommand { get; }
        public ICommand LoadAllFromExcelCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand EditJobInfoCommand { get; }
        public ICommand SetJobInfoCommand { get; }

        public MainWindowViewModel(SectionViewModel sectionViewModel, RebarViewModel rebarViewModel, LoadViewModel loadViewModel, EditJobInfoViewModel editJobInfoViewModel,
            IMainWindowService mainWindowService, IEditStateService editStateService, IWindowService windowService, IGalaService galaService,
            IJobInfoChangedNotifier notifier) 
        {
            SectionView = sectionViewModel;
            RebarView = rebarViewModel;
            LoadView = loadViewModel;
            EditJobInfoView = editJobInfoViewModel;

            _mainWindowService = mainWindowService;
            _editStateService = editStateService;
            _windowService = windowService;
            _galaService = galaService;
            _notifier = notifier;

            SaveAllToExcelCommand = new RelayCommand(param => SaveAllToExcel(), param => HasAll() && !_editStateService.IsEditing);
            LoadAllFromExcelCommand = new RelayCommand(param => LoadAllFromExcel(), param => !_editStateService.IsEditing);
            ClearAllCommand = new RelayCommand(param => ClearAll(), param => HasData() && !_editStateService.IsEditing);
            EditJobInfoCommand = new RelayCommand(param => EditJobInfo());
            SetJobInfoCommand = new RelayCommand(async param => await SetJobInfo());
        }

        private void EditJobInfo()
        {
            _windowService.ShowEditJobInfo(EditJobInfoView.JobInfo, _notifier);
            EditJobInfoView.RefreshFromModel();
        }

        private async Task SetJobInfo()
        {
           await _galaService.HookToGalaJobAsync(EditJobInfoView.JobInfo);
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
