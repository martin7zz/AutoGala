using AutoGala.Common;
using AutoGala.ViewModels.Base;
using Plugin.Core.Models;
using System.Runtime;
using System.Windows.Input;

namespace AutoGala.ViewModels
{
    public class EditJobInfoViewModel : ViewModelBase
    {
        private readonly JobInfo _jobInfo;

        private string? _jobTitle;
        private string? _jobNumber;
        private string? _client;
        private string? _calcsBy;
        private string? _checkedBy;

        public ICommand SaveButtonCommand { get; }

        public event Action? CloseRequested;
        public event Action? SaveRequested;

        public EditJobInfoViewModel(JobInfo jobInfo)
        {
            _jobInfo = jobInfo;

            JobTitle = jobInfo.JobTitle;
            JobNumber = jobInfo.JobNumber;
            Client = jobInfo.Client;
            CalcsBy = jobInfo.CalcsBy;
            CheckedBy = jobInfo.CheckedBy;

            SaveButtonCommand = new RelayCommand(param => SaveJobInfo());
        }

        public JobInfo JobInfo => _jobInfo;

        public string? JobTitle
        {
            get => _jobTitle;
            set
            {
                if (_jobTitle != value)
                {
                    _jobTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? JobNumber
        {
            get => _jobNumber;
            set
            {
                if (_jobNumber != value)
                {
                    _jobNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Client
        {
            get => _client;
            set
            {
                if (_client != value)
                {
                    _client = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? CalcsBy
        {
            get => _calcsBy;
            set
            {
                if (_calcsBy != value)
                {
                    _calcsBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? CheckedBy
        {
            get => _checkedBy;
            set
            {
                if (_checkedBy != value)
                {
                    _checkedBy = value;
                    OnPropertyChanged();
                }
            }
        }

        private void SaveJobInfo()
        {
            _jobInfo.JobTitle = JobTitle;
            _jobInfo.JobNumber = JobNumber;
            _jobInfo.Client = Client;
            _jobInfo.CalcsBy = CalcsBy;
            _jobInfo.CheckedBy = CheckedBy;

            SaveRequested?.Invoke();
        }

        public void RefreshFromModel()
        {
            JobTitle = _jobInfo.JobTitle;
            JobNumber = _jobInfo.JobNumber;
            Client = _jobInfo.Client;
            CalcsBy = _jobInfo.CalcsBy;
            CheckedBy = _jobInfo.CheckedBy;
        }
    }
}