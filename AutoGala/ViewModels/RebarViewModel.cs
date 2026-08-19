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

        private RebarItem? _editingRebar;

        public RebarItem? EditingRebar
        {
            get => _editingRebar;
            private set
            {
                _editingRebar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string EditButtonText =>
            EditingRebar == null ? "Edit" : "Save";

        private int _validationErrorCount;
        public bool HasValidationError => _validationErrorCount > 0;

        private readonly IRebarService _rebarService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;
        private readonly IEditStateService _editStateService;

        public ICommand AddRebarCommand { get; }
        public ICommand RemoveRebarCommand { get; }
        public ICommand PasteRebarCommand { get; }
        public ICommand EditRebarCommand { get; }
        public ICommand MenuEditRebarCommand { get; }
        public ICommand ClearRebarCommand { get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }


        public RebarViewModel(IRebarService rebarService,
            IClipboardService clipboardService,
            IGalaService galaService,
            IWindowService windowService,
            IMainWindowService mainWindowService,
            IEditStateService editStateService)
        {
            _rebarService = rebarService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;
            _editStateService = editStateService;

            AddRebarCommand = new RelayCommand(param => AddRebar(), param => EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
            RemoveRebarCommand = new RelayCommand(param => RemoveRebar(), param => SelectedRebar != null && EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
            PasteRebarCommand = new RelayCommand(param => Paste(), param => EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
            EditRebarCommand = new RelayCommand(param => ToggleEdit(), param => SelectedRebar != null && !HasValidationError && (_editStateService.EditOwner == this || !_editStateService.IsEditing));
            MenuEditRebarCommand = new RelayCommand(param => ToggleEdit(), param => SelectedRebar != null && EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
            ClearRebarCommand = new RelayCommand(param => ClearList(), param => Rebars.Count > 0 && EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
            HookToGalaCommand = new RelayCommand(async param => await GetGala(), param => EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
            SaveToExcelCommand = new RelayCommand(param => SaveToExcel(), param => Rebars.Count > 0 && EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
            LoadFromExcelCommand = new RelayCommand(param => LoadFromExcel(), param => EditingRebar == null && !HasValidationError && !_editStateService.IsEditing);
        }

        private void AddRebar()
        {
            var rebar = _rebarService.CreateRebar();

            rebar.Id = Rebars.Count + 1;

            Rebars.Add(rebar);

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
            if (_selectRebar != null)
            {
                Rebars.Remove(SelectedRebar);

                updateIds();

                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ToggleEdit()
        {
            if (EditingRebar == null)
            {
                EditingRebar = SelectedRebar;
                _editStateService.StartEditing(this);
            }
            else
            {
                SaveRebar();
            }
        }

        private void SaveRebar()
        {
            if (HasValidationError)
            {
                return;
            }

            if (EditingRebar == null ||
            EditingRebar.Area == null || EditingRebar.X == null || EditingRebar.Y == null)
            {
                _windowService.ShowClipboardError(UnfilledRebarErrorMessage);

                return;
            }

            EditingRebar = null;
            _editStateService.StopEditing(this);
        }

        public void ClearList()
        {
            Rebars.Clear();
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

                if (cells.Length < 3 ||
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
