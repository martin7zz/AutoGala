using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.Services;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static AutoGala.Common.NotificationMessages;

namespace AutoGala.ViewModels
{
    public class LoadViewModel : ViewModelBase
    {
        public ObservableCollection<LoadItem> Loads { get; } = new();

        private LoadItem? _selectLoad;
        
        public LoadItem? SelectedLoad
        {
            get => _selectLoad;
            set
            {
                _selectLoad = value;
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
            IsGridReadOnly ? "Edit Load" : "Save Load";

        private readonly ILoadService _loadService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;

        public ICommand AddLoadCommand { get; }
        public ICommand RemoveLoadCommand { get; }
        public ICommand PasteLoadCommand { get; }
        public ICommand EditLoadCommand { get; }
        public ICommand ClearListCommand { get; }
        public ICommand HookToGalaCommand { get; }


        public LoadViewModel(ILoadService loadService, IClipboardService clipboardService, IGalaService galaService, IWindowService windowService)
        {
            _loadService = loadService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            AddLoadCommand = new RelayCommand(_ => AddLoad(), _ => IsGridReadOnly);
            RemoveLoadCommand = new RelayCommand(_ => RemoveLoad(), _ => SelectedLoad != null && IsGridReadOnly);
            PasteLoadCommand = new RelayCommand(_ => Paste(), _ => IsGridReadOnly);
            EditLoadCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedLoad != null);
            ClearListCommand = new RelayCommand(_ => ClearList(), _ => Loads.Count > 0 && IsGridReadOnly);
            HookToGalaCommand = new RelayCommand(async _ => await GetGala(), _ => IsGridReadOnly);
        }

        private void AddLoad()
        {
            Loads.Add(_loadService.CreateLoad());
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveLoad()
        {
            if (_selectLoad != null)
            {
                Loads.Remove(SelectedLoad);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ToggleEdit()
        {
            if (IsGridReadOnly)
            {
                EditLoad();
            }
            else
            {
                SaveLoad();
            }
        }

        private void EditLoad()
        {
            IsGridReadOnly = false;
        }

        private void SaveLoad()
        {
            var InvalidLoads = Loads
                .Where(l => l.N == null || l.Mx == null || l.My == null)
                .ToList();

            if (InvalidLoads.Count > 0)
            {
                _windowService.ShowClipboardError(UnfilledLoadErrorMessage);

                return;
            }

            IsGridReadOnly = true;
        }

        private void ClearList()
        {
            Loads.Clear();
            _loadService.ResetCounter();
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
                    !double.TryParse(cells[0], out var n) ||
                    !double.TryParse(cells[1], out var mx) ||
                    !double.TryParse(cells[2], out var my))
                {
                    failedRows.Add(row);
                    continue;
                }

                Loads.Add(_loadService.CreateLoad(n, mx, my));
                added++;
            }

            if (added == 0)
            {
                _windowService.ShowClipboardError(
                    NotificationMessages.LoadPasteErrorMessage,
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
            await _galaService.HookToGalaAsync(Loads);
        }
    }
}
