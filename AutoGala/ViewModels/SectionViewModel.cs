using AutoGala.Common;
using AutoGala.Contracts;
using AutoGala.ViewModels.Base;
using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace AutoGala.ViewModels
{
    public class SectionViewModel : ViewModelBase
    {
        public ObservableCollection<SectionItem> Sections { get; } = new();

        private SectionItem? _selectSection;

        public SectionItem? SelectedSection
        {
            get => _selectSection;
            set
            {
                _selectSection = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private ObservableCollection<SectionItem> _selectedSections = new();

        public ObservableCollection<SectionItem> SelectedSections
        {
            get => _selectedSections;
            set
            {
                _selectedSections = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private int _validationErrorCount;
        private bool HasValidationError => _validationErrorCount > 0;

        private readonly JobInfo _jobInfo;
        private readonly ISectionService _sectionService;
        private readonly IClipboardService _clipboardService;
        private readonly IGalaService _galaService;
        private readonly IWindowService _windowService;
        private readonly IMainWindowService _mainWindowService;
        private readonly IJobInfoChangedNotifier _notifier;
        private readonly IAutoGalaPipeClientService _autoGalaPipeClientService;
        private readonly IMessageExchangeService _messageExchangeService;
        private readonly IAutoCADOperationRunner _autoCADRunnerService;

        public ICommand AddSectionCommand { get; }
        public ICommand RemoveSectionsCommand { get; }
        public ICommand PasteSectionCommand { get; }
        public ICommand ClearSectionCommand { get; }
        public ICommand HookToGalaCommand { get; }
        public ICommand SaveToExcelCommand { get; }
        public ICommand LoadFromExcelCommand { get; }
        public ICommand GetFromGalaCommand { get; }
        public ICommand GetFromAutoCADCommand { get; }

        public SectionViewModel(ISectionService sectionService,
            IClipboardService clipboardService,
            IGalaService galaService,
            IWindowService windowService,
            IMainWindowService mainWindowService,
            JobInfo jobInfo,
            IJobInfoChangedNotifier notifier,
            IMessageExchangeService messageExchangeService,
            IAutoGalaPipeClientService autoGalaPipeClientService,
            IAutoCADOperationRunner autoCADOperationRunnerService
            )
        {
            _sectionService = sectionService;
            _clipboardService = clipboardService;
            _galaService = galaService;
            _windowService = windowService;
            _mainWindowService = mainWindowService;
            _messageExchangeService = messageExchangeService;
            _autoGalaPipeClientService = autoGalaPipeClientService;
            _autoCADRunnerService = autoCADOperationRunnerService;

            _jobInfo = jobInfo;
            _notifier = notifier;

            _autoGalaPipeClientService.ConnectionStateChanged += () => CommandManager.InvalidateRequerySuggested();

            AddSectionCommand = new RelayCommand(param => AddSection(), param => !HasValidationError);
            RemoveSectionsCommand = new RelayCommand(param => RemoveSections(), param => SelectedSections.Count > 0 && !HasValidationError);
            PasteSectionCommand = new RelayCommand(param => Paste(), param => !HasValidationError);
            ClearSectionCommand = new RelayCommand(param => ClearList(), param => Sections.Count > 0 && !HasValidationError);
            HookToGalaCommand = new RelayCommand(async param => await SetToGalaAsync(), param => !HasValidationError);
            SaveToExcelCommand = new RelayCommand(param => SaveToExcel(), param => Sections.Count > 0 && !HasValidationError);
            LoadFromExcelCommand = new RelayCommand(param => LoadFromExcel(), param => !HasValidationError);
            GetFromGalaCommand = new RelayCommand(async param => await GetFromGalaAsync(), param => !HasValidationError);
            GetFromAutoCADCommand = new RelayCommand(async param => await GetFromAutoCAD(), param => !HasValidationError && _autoGalaPipeClientService.IsConnected);
        }

        private void AddSection()
        {
            var section = _sectionService.CreateSection();

            section.Id = Sections.Count + 1;

            Sections.Add(section);

            CommandManager.InvalidateRequerySuggested();
        }

        private void updateIds()
        {
            int id = 1;
            foreach (var section in Sections)
            {
                section.Id = id++;
            }

            var items = Sections.ToList();

            Sections.Clear();

            foreach (var section in items)
            {
                Sections.Add(section);
            }
        }

        public void RemoveSections()
        {
            foreach (var section in SelectedSections.ToList())
            {
                Sections.Remove(section);
            }

            updateIds();

            SelectedSections.Clear();

            CommandManager.InvalidateRequerySuggested();
        }

        public void ClearList()
        {
            Sections.Clear();
            CommandManager.InvalidateRequerySuggested();
        }

        private void Paste()
        {
            var clipboard = _clipboardService.GetText();

            if (string.IsNullOrWhiteSpace(clipboard))
            {
                _windowService.ShowError(NotificationMessages.NoClipboardDataErrorMassage);
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

                if (cells.Length != 2 ||
                    !double.TryParse(cells[0], out var x) ||
                    !double.TryParse(cells[1], out var y))
                {
                    failedRows.Add(row);
                    continue;
                }

                var section = _sectionService.CreateSection(x, y);

                section.Id = Sections.Count + 1;

                Sections.Add(section);
                added++;
            }

            if (added == 0)
            {
                _windowService.ShowError(
                    NotificationMessages.SectionPasteErrorMessage,
                    failedRows);
            }
            else if (failedRows.Count > 0)
            {
                _windowService.ShowError(
                    $"{added} row(s) added, but {failedRows.Count} row(s) couldn't be parsed.",
                    failedRows);
            }
        }

        private async Task SetToGalaAsync()
        {
            try
            {
                await _galaService.HookToGalaAsync(Sections);
            }
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError(ex.Message);
                return;
            }

            CommandManager.InvalidateRequerySuggested();
        }
        private async Task GetFromGalaAsync()
        {
            ObservableCollection<SectionItem> sections;

            try
            {
                sections = await _galaService.GetSectionsFromGalaAsync();
            }
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError(ex.Message);
                return;
            }

            if (sections.Any())
            {
                Sections.Clear();
            }

            foreach (var section in sections)
            {
                Sections.Add(section);
            }


            CommandManager.InvalidateRequerySuggested();
        }

        private async Task GetFromAutoCAD()
        {
            var (success, sections) = await _autoCADRunnerService.RunAsync(() =>
            _messageExchangeService.GetSectionsAsync(_autoGalaPipeClientService, _jobInfo.JobTitle));

            if (!success)
            {
                return;
            }

            if (sections.Item1.Any())
            {
                Sections.Clear();
            }

            foreach (var section in sections.Item1)
            {
                Sections.Add(section);
            }

            _jobInfo.JobTitle = sections.Item2;
            _notifier.NotifyJobInfoChanged();

            CommandManager.InvalidateRequerySuggested();
        }

        private void SaveToExcel()
        {
            try
            {
                _mainWindowService.SaveExcel(Sections, _jobInfo);
            }
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError($"File was not saved: {ex.Message}");
                return;
            }
            
            CommandManager.InvalidateRequerySuggested();
        }

        private void LoadFromExcel()
        {
            ObservableCollection<SectionItem> loadedSections;
            try
            {
                loadedSections = _mainWindowService.LoadSectionsExcel(_jobInfo);
            }
            catch (InvalidOperationException ex)
            {
                _windowService.ShowError($"Unable to load file: {ex.Message}");
                return;
            }

            if (loadedSections.Count > 0)
            {
                ClearList();
            }

            foreach (var section in loadedSections)
            {
                Sections.Add(section);
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
