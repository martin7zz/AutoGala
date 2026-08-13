using AutoGala.Common;
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


        private IMainWindowService _mainWindowService;

        public ICommand SaveAllToExcelCommand { get; }
        public ICommand LoadAllFromExcelCommand { get; }
        public ICommand ClearAllCommand { get; }

        public MainWindowViewModel(SectionViewModel sectionView, RebarViewModel rebarView, LoadViewModel loadView, IMainWindowService mainWindowService) 
        {
            SectionView = sectionView;
            RebarView = rebarView;
            LoadView = loadView;

            _mainWindowService = mainWindowService;

            SaveAllToExcelCommand = new RelayCommand(param => SaveAllToExcel(), param => HasAll());
            LoadAllFromExcelCommand = new RelayCommand(param => LoadAllFromExcel());
            ClearAllCommand = new RelayCommand(param => ClearAll(), param => HasData());
        }

        private void SaveAllToExcel()
        {
            _mainWindowService.SaveAllToExcel(
                SectionView.Sections,
                RebarView.Rebars,
                LoadView.Loads);
        }

        private void LoadAllFromExcel()
        {
            var items = _mainWindowService.LoadAllExcel();

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
