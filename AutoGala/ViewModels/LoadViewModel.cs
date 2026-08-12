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

        public LoadItem? _editingLoad;

        public LoadItem? EditingLoad
        {
            get => _editingLoad;
            private set
            {
                _editingLoad = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string EditButtonText => EditingLoad == null ? "Edit" : "Save";

        private int _validationErrorCount;
        public bool HasValidationError => _validationErrorCount > 0;

        private readonly ILoadService _loadService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;

        public ICommand AddLoadCommand { get; }
        public ICommand RemoveLoadCommand { get; }
        public ICommand PasteLoadCommand { get; }
        public ICommand EditLoadCommand { get; }
        public ICommand MenuEditLoadCommand { get; }
        public ICommand ClearListCommand { get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }



        public LoadViewModel(ILoadService loadService, IClipboardService clipboardService, IGalaService galaService, IWindowService windowService, IMainWindowService mainWindowService)
        {
            _loadService = loadService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;

            AddLoadCommand = new RelayCommand(_ => AddLoad(), _ => EditingLoad == null && !HasValidationError);
            RemoveLoadCommand = new RelayCommand(_ => RemoveLoad(), _ => SelectedLoad != null && EditingLoad == null && !HasValidationError);
            PasteLoadCommand = new RelayCommand(_ => Paste(), _ => EditingLoad == null && !HasValidationError);
            EditLoadCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedLoad != null && !HasValidationError);
            ClearListCommand = new RelayCommand(_ => ClearList(), _ => Loads.Count > 0 && EditingLoad == null && !HasValidationError);
            HookToGalaCommand = new RelayCommand(async _ => await GetGala(), _ => EditingLoad == null && !HasValidationError);
            SaveToExcelCommand = new RelayCommand(_ => SaveToExcel(), _ => Loads.Count > 0 && EditingLoad == null && !HasValidationError);
            LoadFromExcelCommand = new RelayCommand(_ => LoadFromExcel(), _ => EditingLoad == null && !HasValidationError);
            MenuEditLoadCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedLoad != null && EditingLoad == null && !HasValidationError);
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
            if (EditingLoad == null)
            {
                EditingLoad = SelectedLoad;
            }
            else
            {
                SaveLoad();
            }
        }

        private void SaveLoad()
        {
            if (HasValidationError) return;

            if (EditingLoad == null ||
            EditingLoad.N == null || EditingLoad.Mx == null || EditingLoad.My == null)
            {
                _windowService.ShowClipboardError(UnfilledLoadErrorMessage);
                return;
            }

            EditingLoad = null;
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

        private void SaveToExcel()
        {
            _mainWindowService.SaveExcel(Loads);
        }

        private void LoadFromExcel()
        {
            if (Loads.Count > 0)
            {
                Loads.Clear();
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
