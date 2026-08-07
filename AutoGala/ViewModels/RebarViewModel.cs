using AutoGala.Common;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

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
            }
        }

        public string EditButtonText =>
            IsGridReadOnly ? "Edit Rebar" : "Save Rebar";

        private readonly IRebarService _rebarService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;

        public ICommand AddRebarCommand { get; }
        public ICommand RemoveRebarCommand { get; }
        public ICommand PasteRebarCommand { get; }
        public ICommand EditRebarCommand { get; }
        public ICommand ClearRebarCommand { get; }
        public ICommand HookToGalaCommand { get; }


        public RebarViewModel(IRebarService rebarService, IClipboardService clipboardService, IGalaService galaService)
        {
            _rebarService = rebarService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            AddRebarCommand = new RelayCommand(_ => AddRebar());
            RemoveRebarCommand = new RelayCommand(_ => RemoveRebar(), _ => SelectedRebar != null);
            PasteRebarCommand = new RelayCommand(_ => Paste());
            EditRebarCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedRebar != null);
            ClearRebarCommand = new RelayCommand(_ => ClearList(), _ => Rebars.Count > 0);
            HookToGalaCommand = new RelayCommand(async _ => await GetGala());
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
                return;
            }

            var rows = clipboard.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var row in rows)
            {
                var cells = row.Split('\t');

                if (cells.Length < 3)
                {
                    continue;
                }

                if (double.TryParse(cells[0], out var area) &&
                    double.TryParse(cells[1], out var x) &&
                    double.TryParse(cells[2], out var y))
                {
                    Rebars.Add(_rebarService.CreateRebar(area, x, y));
                }
            }
        }

        private async Task GetGala()
        {
            await _galaService.HookToGalaAsync(Rebars);
        }
    }
}
