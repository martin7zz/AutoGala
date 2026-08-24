using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using Plugin.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using static AutoGala.Common.NotificationMessages;

namespace AutoGala.ViewModels
{
    public class RebarViewModel : ViewModelBase
    {
        public ObservableCollection<RebarItem> Rebars { get; } = new();

        private RebarItem? _selectRebar;

        public RebarItem? SelectedRebar
        {
            get => _selectRebar;
            set
            {
                _selectRebar = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private ObservableCollection<RebarItem> _selectedRebars = new();

        public ObservableCollection<RebarItem> SelectedRebars
        {
            get => _selectedRebars;
            set
            { 
                _selectedRebars = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private ObservableCollection<RebarItem> _duplicateRebars = new();

        public ObservableCollection<RebarItem> DuplicateRebars
        {
            get => _duplicateRebars;
            set
            {
                _duplicateRebars = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private int _validationErrorCount;
        public bool HasValidationError => _validationErrorCount > 0;

        private readonly IRebarService _rebarService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;

        public ICommand AddRebarCommand { get; }
        public ICommand RemoveRebarsCommand { get; }
        public ICommand PasteRebarCommand { get; }
        public ICommand ClearRebarCommand { get; }
        public ICommand CheckForDuplicatesCommand {  get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }

        public RebarViewModel(IRebarService rebarService,
            IClipboardService clipboardService,
            IGalaService galaService,
            IWindowService windowService,
            IMainWindowService mainWindowService)
        {
            _rebarService = rebarService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;

            AddRebarCommand = new RelayCommand(param => AddRebar(), param => !HasValidationError);
            RemoveRebarsCommand = new RelayCommand(param => RemoveRebar(), param => SelectedRebars.Count > 0 && !HasValidationError);
            PasteRebarCommand = new RelayCommand(param => Paste(), param => !HasValidationError);
            ClearRebarCommand = new RelayCommand(param => ClearList(), param => Rebars.Count > 0 && !HasValidationError);
            CheckForDuplicatesCommand = new RelayCommand(param =>  CheckForDuplicates(), param => Rebars.Count > 0);
            HookToGalaCommand = new RelayCommand(async param => await GetGala(), param => !HasValidationError);
            SaveToExcelCommand = new RelayCommand(param => SaveToExcel(), param => Rebars.Count > 0 && !HasValidationError);
            LoadFromExcelCommand = new RelayCommand(param => LoadFromExcel(), param => !HasValidationError);
        }

        private void AddRebar()
        {
            var rebar = _rebarService.CreateRebar();

            rebar.Id = Rebars.Count + 1;

            Rebars.Add(rebar);

            CheckForDuplicates();

            CommandManager.InvalidateRequerySuggested();
        }

        private void updateIds()
        {
            int id = 1;
            foreach (var rebar in Rebars)
            {
                rebar.Id = id++;
            }

            var items = Rebars.ToList();

            Rebars.Clear();

            foreach (var section in items)
            {
                Rebars.Add(section);
            }
        }

        private void RemoveRebar()
        {
            foreach (var rebar in SelectedRebars.ToList())
            {
                Rebars.Remove(rebar);
            }

            updateIds();

            SelectedRebars.Clear();

            CheckForDuplicates();

            CommandManager.InvalidateRequerySuggested();
        }

        public void ClearList()
        {
            Rebars.Clear();
            CommandManager.InvalidateRequerySuggested();
        }

        public void CheckForDuplicates()
        {
            var duplicateGroups = Rebars
                .GroupBy(r => (r.Area, r.X, r.Y))
                .Where(g => g.Count() > 1);

            DuplicateRebars = new ObservableCollection<RebarItem>(
                duplicateGroups.SelectMany(g => g.Skip(1))
            );
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

                if (cells.Length != 3 ||
                    !double.TryParse(cells[0], out var area) ||
                    !double.TryParse(cells[1], out var x) ||
                    !double.TryParse(cells[2], out var y))
                {
                    failedRows.Add(row);
                    continue;
                }

                var rebar = _rebarService.CreateRebar(area, x, y);

                rebar.Id = Rebars.Count + 1;

                Rebars.Add(rebar);

                added++;
            }

            if (added == 0)
            {
                _windowService.ShowClipboardError(
                    NotificationMessages.RebarPasteErrorMessage,
                    failedRows);
            }
            else if (failedRows.Count > 0)
            {
                _windowService.ShowClipboardError(
                    $"{added} row(s) added, but {failedRows.Count} row(s) couldn't be parsed.",
                    failedRows);
            }

            CheckForDuplicates();
        }

        private async Task GetGala()
        {
            await _galaService.HookToGalaAsync(Rebars);
        }

        private void SaveToExcel()
        {
            _mainWindowService.SaveExcel(Rebars);
        }

        private void LoadFromExcel()
        {
            var loadedRebars = _mainWindowService.LoadRebarsExcel();

            if (loadedRebars.Count > 0)
            {
                ClearList();
            }

            foreach (var rebar in loadedRebars)
            {
                Rebars.Add(rebar);
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
