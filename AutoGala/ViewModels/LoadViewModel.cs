using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
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

        private ObservableCollection<LoadItem> _selectedLoads = new();

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

        private bool _isSimpleBending = false;

        public bool IsSimpleBending
        {
            get => _isSimpleBending;
            set
            {
                if (_isSimpleBending != value)
                {
                    _isSimpleBending = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _validationErrorCount;
        public bool HasValidationError => _validationErrorCount > 0;

        private readonly JobInfo _jobInfo;

        private readonly ILoadService _loadService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;
        private readonly IJobInfoChangedNotifier _notifier;

        public ICommand AddLoadCommand { get; }
        public ICommand RemoveLoadsCommand { get; }
        public ICommand PasteLoadCommand { get; }
        public ICommand ClearLoadCommand { get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }
        public ICommand ReverseMxSignCommand { get; }
        public ICommand ReverseMySignCommand { get; }
        public ICommand ReverseMxAndMyCommand { get; }
        public ICommand SimpleBendingCommand { get; }
        public ICommand RemoveZeroLoadCasesCommand { get; }
        public ICommand GetFromGalaCommand { get; }

        public LoadViewModel(ILoadService loadService,
            IClipboardService clipboardService,
            IGalaService galaService,
            IWindowService windowService,
            IMainWindowService mainWindowService,
            JobInfo jobInfo,
            IJobInfoChangedNotifier notifier)
        {
            _loadService = loadService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;
            _jobInfo = jobInfo;
            _notifier = notifier;

            AddLoadCommand = new RelayCommand(param => AddLoad(), param => !HasValidationError);
            RemoveLoadsCommand = new RelayCommand(param => RemoveLoad(), param => SelectedLoad != null && !HasValidationError);
            PasteLoadCommand = new RelayCommand(param => Paste(), param => !HasValidationError);
            ClearLoadCommand = new RelayCommand(param => ClearList(), param => Loads.Count > 0 && !HasValidationError);
            HookToGalaCommand = new RelayCommand(async param => await SetToGalaAsync(), param => !HasValidationError);
            SaveToExcelCommand = new RelayCommand(param => SaveToExcel(), param => Loads.Count > 0 && !HasValidationError);
            LoadFromExcelCommand = new RelayCommand(param => LoadFromExcel(), param => !HasValidationError);
            ReverseMxSignCommand = new RelayCommand(param => ReverseMxSign(), param => Loads.Count > 0 && !HasValidationError);
            ReverseMySignCommand = new RelayCommand(param => ReverseMySign(), param => Loads.Count > 0 && !HasValidationError && !IsSimpleBending);
            ReverseMxAndMyCommand = new RelayCommand(param => ReverseMxAndMy(), param => Loads.Count > 0 && !HasValidationError && !IsSimpleBending);
            SimpleBendingCommand = new RelayCommand(param => SetIsSimpleBending(), param => !HasValidationError);
            RemoveZeroLoadCasesCommand = new RelayCommand(param => DeleteZeroLoads(), param => Loads.Count > 0 && !HasValidationError);
            GetFromGalaCommand = new RelayCommand(async param => await GetFromGalaAsync(), param => !HasValidationError);
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

            updateLoadsTable();
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

        private void DeleteZeroLoads()
        {
            foreach (var load in Loads.ToList())
            {
                if (load.N == 0 && load.Mx == 0 && load.My == 0)
                {
                    Loads.Remove(load);
                }
            }

            updateIds();

            CommandManager.InvalidateRequerySuggested();
        }

        public void ClearList()
        {
            Loads.Clear();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ReverseMxSign()
        {
            foreach (var load in Loads)
            {
                load.Mx *= -1;
            }

            updateLoadsTable();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ReverseMySign()
        {
            foreach (var load in Loads)
            {
                load.My *= -1;
            }

            updateLoadsTable();
            CommandManager.InvalidateRequerySuggested();
        }

        private void ReverseMxAndMy()
        {
            foreach (var load in Loads)
            {
                (load.Mx, load.My) = (load.My, load.Mx);
            }

            updateLoadsTable();
            CommandManager.InvalidateRequerySuggested();
        }

        private void updateLoadsTable()
        {
            var items = Loads.ToList();

            Loads.Clear();

            foreach (var load in items)
            {
                Loads.Add(load);
            }
        }

        private void SetIsSimpleBending()
        {
            IsSimpleBending = !IsSimpleBending;
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

                if (!double.TryParse(cells[0], out var n) ||
                    !double.TryParse(cells[1], out var mx) ||
                    (IsSimpleBending
                        ? cells.Length != 2
                        : cells.Length != 3))
                {
                    failedRows.Add(row);
                    continue;
                }

                double my = 0;

                if (!IsSimpleBending && !double.TryParse(cells[2], out my))
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
                var message = IsSimpleBending ?
                    NotificationMessages.LoadSimpleBendingPasteErrorMessage : NotificationMessages.LoadPasteErrorMessage;

                _windowService.ShowClipboardError(
                    message,
                    failedRows);
            }
            else if (failedRows.Count > 0)
            {
                _windowService.ShowClipboardError(
                    $"{added} row(s) added, but {failedRows.Count} row(s) couldn't be parsed.",
                    failedRows);
            }
        }

        private async Task SetToGalaAsync()
        {
            await _galaService.HookToGalaAsync(Loads, IsSimpleBending);

            CommandManager.InvalidateRequerySuggested();
        }

        private async Task GetFromGalaAsync()
        {
            var loads = await _galaService.GetLoadsFromGalaAsync(IsSimpleBending);

            if (loads.Any())
            {
                Loads.Clear();
            }

            foreach (var load in loads)
            {
                Loads.Add(load);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void SaveToExcel()
        {
            _mainWindowService.SaveExcel(Loads, IsSimpleBending, _jobInfo);
        }

        private void LoadFromExcel()
        {
            var loadedLoads = _mainWindowService.LoadLoadsExcel(IsSimpleBending, _jobInfo);

            if (loadedLoads.Count > 0)
            {
                ClearList();
            }

            foreach (var load in loadedLoads)
            {
                Loads.Add(load);
            }

            _notifier.NotifyJobInfoChanged();

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
