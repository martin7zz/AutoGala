using Plugin.Core.Models;
using AutoGala.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace AutoGala.ViewModels
{
    public class LoadViewModel : INotifyPropertyChanged
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
            }
        }

        private int _nextId = 1;

        public ICommand AddLoadCommand { get; }
        public ICommand RemoveLoadCommand { get; }

        public LoadViewModel()
        {
            AddLoadCommand = new RelayCommand(_ => AddLoad());
            RemoveLoadCommand = new RelayCommand(_ => RemoveLoad(), _ => SelectedLoad != null);
        }

        private void AddLoad()
        {
            Loads.Add(new LoadItem { Id = _nextId++, N = 0, Mx = 0, My = 0 });
        }

        private void RemoveLoad()
        {
            if (_selectLoad != null)
            {
                Loads.Remove(SelectedLoad);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
