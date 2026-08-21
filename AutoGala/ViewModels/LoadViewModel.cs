using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.Services;
using AutoGala.ViewModels.Base;
using DocumentFormat.OpenXml.Drawing.Charts;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using Plugin.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using static AutoGala.Common.NotificationMessages;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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

        private ObservableCollection<LoadItem> _selectedLoads = new ();

        public ObservableCollection<LoadItem> SelectedLoads
        {
            get => _selectedLoads;
            set
            {
                _selectedLoads = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private int _validationErrorCount;
        public bool HasValidationError => _validationErrorCount > 0;

        private readonly ILoadService _loadService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;

        public ICommand AddLoadCommand { get; }
        public ICommand RemoveLoadsCommand { get; }
        public ICommand PasteLoadCommand { get; }
        public ICommand ClearLoadCommand { get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }

        public LoadViewModel(ILoadService loadService,
            IClipboardService clipboardService, 
            IGalaService galaService, 
            IWindowService windowService, 
            IMainWindowService mainWindowService)
        {
            _loadService = loadService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;

            AddLoadCommand = new RelayCommand(param => AddLoad(), param => !HasValidationError);
            RemoveLoadsCommand = new RelayCommand(param => RemoveLoad(), param => SelectedLoad != null && !HasValidationError);
            PasteLoadCommand = new RelayCommand(param => Paste(), param => !HasValidationError);
            ClearLoadCommand = new RelayCommand(param => ClearList(), param => Loads.Count > 0 && !HasValidationError);
            HookToGalaCommand = new RelayCommand(async param => await GetGala(), param => !HasValidationError);
            SaveToExcelCommand = new RelayCommand(param => SaveToExcel(), param => Loads.Count > 0 && !HasValidationError);
            LoadFromExcelCommand = new RelayCommand(param => LoadFromExcel(), param => !HasValidationError);
        }

        private void AddLoad()
        {
            var load = _loadService.CreateLoad();

            load.Id = Loads.Count + 1;

            Loads.Add(load);

            CommandManager.InvalidateRequerySuggested();
        }

        private void updateIds()
        {
            int id = 1;
            foreach (var load in Loads)
            {
                load.Id = id++;
            }

            var items = Loads.ToList();

            Loads.Clear();

            foreach (var section in items)
            {
                Loads.Add(section);
            }
        }

        private void RemoveLoad()
        {
            foreach (var load in SelectedLoads.ToList())
            {
                Loads.Remove(load);
            }

            updateIds();

            SelectedLoads.Clear();

            CommandManager.InvalidateRequerySuggested();
        }

        public void ClearList()
        {
            Loads.Clear();
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

                var load = _loadService.CreateLoad(n, mx, my);

                load.Id = Loads.Count + 1;

                Loads.Add(load);

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
            var loadedLoads = _mainWindowService.LoadLoadsExcel();

            if (loadedLoads.Count > 0)
            {
                ClearList();
            }

            foreach (var load in loadedLoads)
            {
                Loads.Add(load);
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
