using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Text;
using System.Windows.Input;
using static AutoGala.Common.NotificationMessages;

namespace AutoGala.ViewModels
{
    public class SectionViewModel : ViewModelBase
    {
        public ObservableCollection<SectionItem> Sections { get; } = new();

        private SectionItem? _selectSection;

        public SectionItem? SelectedSection
        {
            get => _selectSection;
            set
            {
                _selectSection = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private ObservableCollection<SectionItem> _selectedSections = new();

        public ObservableCollection<SectionItem> SelectedSections
        {
            get => _selectedSections;
            set
            {
                _selectedSections = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private int _validationErrorCount;
        private bool HasValidationError => _validationErrorCount > 0;

        private readonly ISectionService _sectionService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;
        public ICommand AddSectionCommand { get; }
        public ICommand RemoveSectionsCommand { get; }
        public ICommand PasteSectionCommand { get; }
        public ICommand ClearSectionCommand { get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }


        public SectionViewModel(ISectionService sectionService,
            IClipboardService clipboardService,
            IGalaService galaService,
            IWindowService windowService,
            IMainWindowService mainWindowService)
        {
            _sectionService = sectionService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;

            AddSectionCommand = new RelayCommand(param => AddSection(), param => !HasValidationError);
            RemoveSectionsCommand = new RelayCommand(param => RemoveSections(), param => SelectedSections.Count > 0 && !HasValidationError);
            PasteSectionCommand = new RelayCommand(param => Paste(), param => !HasValidationError);
            ClearSectionCommand = new RelayCommand(param => ClearList(), param => Sections.Count > 0 && !HasValidationError);
            HookToGalaCommand = new RelayCommand(async param => await GetGala(), param => !HasValidationError);
            SaveToExcelCommand = new RelayCommand(param => SaveToExcel(), param => Sections.Count > 0 && !HasValidationError);
            LoadFromExcelCommand = new RelayCommand(param => LoadFromExcel(),param => !HasValidationError);
        }

        private void AddSection()
        {
            var section = _sectionService.CreateSection();

            section.Id = Sections.Count + 1;

            Sections.Add(section);

            CommandManager.InvalidateRequerySuggested();
        }

        private void updateIds()
        {
            int id = 1;
            foreach (var section in Sections)
            {
                section.Id = id++;
            }

            var items = Sections.ToList();

            Sections.Clear();

            foreach (var section in items)
            {
                Sections.Add(section);
            }
        }

        private void RemoveSections()
        {
            foreach (var section in SelectedSections.ToList())
            {
                Sections.Remove(section);
            }

            updateIds();

            SelectedSections.Clear();

            CommandManager.InvalidateRequerySuggested();
        }

        public void ClearList()
        {
            Sections.Clear();
            CommandManager.InvalidateRequerySuggested();
        }

        private void Paste()
        {
            var clipboard = _clipboardService.GetText();

            if (string.IsNullOrWhiteSpace(clipboard))
            {
                _windowService.ShowClipboardError(NotificationMessages.NoClipboardDataErrorMassage);
                return;
            }

            var rows = clipboard.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            int added = 0;
            var failedRows = new List<string>();

            foreach (var row in rows)
            {
                var cells = row.Split('\t');

                if (cells.Length <= 1 || cells.Length >= 3 ||
                    !double.TryParse(cells[0], out var x) ||
                    !double.TryParse(cells[1], out var y))
                {
                    failedRows.Add(row);
                    continue;
                }

                var section = _sectionService.CreateSection(x, y);

                section.Id = Sections.Count + 1;

                Sections.Add(section);
                added++;
            }

            if (added == 0)
            {
                _windowService.ShowClipboardError(
                    NotificationMessages.SectionPasteErrorMessage,
                    failedRows);
            }
            else if (failedRows.Count > 0)
            {
                _windowService.ShowClipboardError(
                    $"{added} row(s) added, but {failedRows.Count} row(s) couldn't be parsed.",
                    failedRows);
            }
        }

        private async Task GetGala()
        {
            await _galaService.HookToGalaAsync(Sections);
        }

        private void SaveToExcel()
        {
            _mainWindowService.SaveExcel(Sections);
        }

        private void LoadFromExcel()
        {
            var loadedSections = _mainWindowService.LoadSectionsExcel();

            if (loadedSections.Count > 0)
            {
                ClearList();
            }

            foreach (var section in loadedSections)
            {
                Sections.Add(section);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        public void RegisterValidationError(bool errorAdded)
        {
            _validationErrorCount += errorAdded ? 1 : -1;
            if (_validationErrorCount < 0)
            {
                _validationErrorCount = 0;
            }
            OnPropertyChanged(nameof(HasValidationError));
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
