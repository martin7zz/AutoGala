using AutoGala.Common;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Text;
using System.Windows.Input;

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

        private bool _isGridReadOnly = true;

        public bool IsGridReadOnly
        {
            get => _isGridReadOnly;
            set
            {
                _isGridReadOnly = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditButtonText));
            }
        }

        public string EditButtonText =>
            IsGridReadOnly ? "Edit Section" : "Save Section";

        private readonly ISectionService _sectionService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;

        public ICommand AddSectionCommand { get; }
        public ICommand RemoveSectionCommand { get; }
        public ICommand PasteSectionCommand { get; }
        public ICommand EditSectionCommand { get; }
        public ICommand ClearSectionCommand { get; }
        public ICommand HookToGalaCommand { get; }


        public SectionViewModel(ISectionService sectionService, IClipboardService clipboardService, IGalaService galaService)
        {
            _sectionService = sectionService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            AddSectionCommand = new RelayCommand(_ => AddSection());
            RemoveSectionCommand = new RelayCommand(_ => RemoveSection(), _ => SelectedSection != null);
            PasteSectionCommand = new RelayCommand(_ => Paste());
            EditSectionCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedSection != null);
            ClearSectionCommand = new RelayCommand(_ => ClearList(), _ => Sections.Count > 0);
            HookToGalaCommand = new RelayCommand(async _ => await GetGala());
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
            if (IsGridReadOnly)
            {
                EditSection();
            }
            else
            {
                SaveSection();
            }
        }

        private void EditSection()
        {
            IsGridReadOnly = false;
        }

        private void SaveSection()
        {
            IsGridReadOnly = true;
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
                return;
            }

            var rows = clipboard.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var row in rows)
            {
                var cells = row.Split('\t');

                if (cells.Length < 2)
                {
                    continue;
                }

                if (double.TryParse(cells[0], out var x) &&
                    double.TryParse(cells[1], out var y))
                {
                    Sections.Add(_sectionService.CreateSection(x, y));
                }
            }
        }

        private async Task GetGala()
        {
            await _galaService.HookToGalaAsync(Sections);
        }
    }
}
