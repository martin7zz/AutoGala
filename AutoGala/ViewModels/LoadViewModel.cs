using AutoGala.Common;
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
            }
        }

        public string EditButtonText =>
            IsGridReadOnly ? "Edit Load" : "Save Load";

        private readonly ILoadService _loadService;
        private IClipboardService _clipboardService;

        public ICommand AddLoadCommand { get; }
        public ICommand RemoveLoadCommand { get; }
        public ICommand PasteLoadCommand { get; }
        public ICommand EditLoadCommand { get; }
        public ICommand ClearListCommand { get; }

        public LoadViewModel(ILoadService loadService, IClipboardService clipboardService)
        {
            _loadService = loadService;
            _clipboardService = clipboardService;
            AddLoadCommand = new RelayCommand(_ => AddLoad());
            RemoveLoadCommand = new RelayCommand(_ => RemoveLoad(), _ => SelectedLoad != null);
            PasteLoadCommand = new RelayCommand(_ => Paste());
            EditLoadCommand = new RelayCommand(_ => ToggleEdit(), _ => SelectedLoad != null);
            ClearListCommand = new RelayCommand(_ => ClearList(), _ => Loads.Count > 0);
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

                if (double.TryParse(cells[0], out var n) &&
                    double.TryParse(cells[1], out var mx) &&
                    double.TryParse(cells[2], out var my))
                {
                    Loads.Add(_loadService.CreateLoad(n, mx, my));
                }
            }
        }
    }
}
