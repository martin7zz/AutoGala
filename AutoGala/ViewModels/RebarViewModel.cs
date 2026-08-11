using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
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

        private bool _isGridReadOnly = true;

        public bool IsGridReadOnly
        {
            get => _isGridReadOnly;
            set
            {
                _isGridReadOnly = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string EditButtonText =>
            IsGridReadOnly ? "Edit Rebar" : "Save Rebar";

        private readonly IRebarService _rebarService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;

        public ICommand AddRebarCommand { get; }
        public ICommand RemoveRebarCommand { get; }
        public ICommand PasteRebarCommand { get; }
        public ICommand EditRebarCommand { get; }
        public ICommand ClearRebarCommand { get; }
        public ICommand HookToGalaCommand { get; }


        public RebarViewModel(IRebarService rebarService, IClipboardService clipboardService, IGalaService galaService, IWindowService windowService)
        {
            _rebarService = rebarService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            AddRebarCommand = new RelayCommand(_ => AddRebar(), _ => IsGridReadOnly);
            RemoveRebarCommand = new RelayCommand(_ => RemoveRebar(), _ => SelectedRebar != null && IsGridReadOnly);
            PasteRebarCommand = new RelayCommand(_ => Paste(), _ => IsGridReadOnly);
            EditRebarCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedRebar != null);
            ClearRebarCommand = new RelayCommand(_ => ClearList(), _ => Rebars.Count > 0 && IsGridReadOnly);
            HookToGalaCommand = new RelayCommand(async _ => await GetGala(), _ => IsGridReadOnly);
        }

        private void AddRebar()
        {
            Rebars.Add(_rebarService.CreateRebar());
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveRebar()
        {
            if (_selectRebar != null)
            {
                Rebars.Remove(SelectedRebar);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ToggleEdit()
        {
            if (IsGridReadOnly)
            {
                EditRebar();
            }
            else
            {
                SaveRebar();
            }
        }

        private void EditRebar()
        {
            IsGridReadOnly = false;
        }

        private void SaveRebar()
        {
            var InvalidRebars = Rebars
                .Where(r => r.X == null || r.Y == null || r.Area == null)
                .ToList();

            if (InvalidRebars.Count > 0)
            {
                _windowService.ShowClipboardError(UnfilledRebarErrorMessage);

                return;
            }

            IsGridReadOnly = true;
        }

        private void ClearList()
        {
            Rebars.Clear();
            _rebarService.ResetCounter();
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
                
                Rebars.Add(_rebarService.CreateRebar(area, x, y));
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
    }
}
