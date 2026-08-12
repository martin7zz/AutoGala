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

        private SectionItem? _editingSection;
        public SectionItem? EditingSection
        {
            get => _editingSection;
            private set
            {
                _editingSection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string EditButtonText =>
            EditingSection == null ? "Edit" : "Save";

        private int _validationErrorCount;
        private bool HasValidationError => _validationErrorCount > 0;

        private readonly ISectionService _sectionService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;

        public ICommand AddSectionCommand { get; }
        public ICommand RemoveSectionCommand { get; }
        public ICommand PasteSectionCommand { get; }
        public ICommand EditSectionCommand { get; }
        public ICommand MenuEditSectionCommand { get; }
        public ICommand ClearSectionCommand { get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }


        public SectionViewModel(ISectionService sectionService, IClipboardService clipboardService, IGalaService galaService, IWindowService windowService, IMainWindowService mainWindowService)
        {
            _sectionService = sectionService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;

            AddSectionCommand = new RelayCommand(_ => AddSection(), _ => EditingSection == null && !HasValidationError);
            RemoveSectionCommand = new RelayCommand(_ => RemoveSection(), _ => SelectedSection != null && EditingSection == null && !HasValidationError);
            PasteSectionCommand = new RelayCommand(_ => Paste(), _ => EditingSection == null && !HasValidationError);
            EditSectionCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedSection != null && !HasValidationError);
            MenuEditSectionCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedSection != null && EditingSection == null && !HasValidationError);
            ClearSectionCommand = new RelayCommand(_ => ClearList(), _ => Sections.Count > 0 && EditingSection == null && !HasValidationError);
            HookToGalaCommand = new RelayCommand(async _ => await GetGala(), _ => EditingSection == null && !HasValidationError);
            SaveToExcelCommand = new RelayCommand(_ => SaveToExcel(), _ => Sections.Count > 0 && EditingSection == null && !HasValidationError);
            LoadFromExcelCommand = new RelayCommand(_ => LoadFromExcel(),_ => EditingSection == null && !HasValidationError);
        }

        private void AddSection()
        {
            Sections.Add(_sectionService.CreateSection());
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveSection()
        {
            if (_selectSection != null)
            {
                Sections.Remove(SelectedSection);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ToggleEdit()
        {
            if (EditingSection == null)
            {
                EditingSection = SelectedSection;
            }
            else
            {
                SaveSection();
            }
        }

        private void SaveSection()
        {
            if (HasValidationError) return;

            if (EditingSection == null || EditingSection.X == null || EditingSection.Y == null)
            {
                _windowService.ShowClipboardError(UnfilledSectionErrorMessage);

                return;
            }

            EditingSection = null;
        }

        private void ClearList()
        {
            Sections.Clear();
            _sectionService.ResetCounter();
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

                Sections.Add(_sectionService.CreateSection(x, y));
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
            if (Sections.Count > 0)
            {
                Sections.Clear();
            }


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
