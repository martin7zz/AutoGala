using AutoGala.views;
using Plugin.Core.Models;
using System.Windows;

namespace AutoGala.Contracts
{
    public interface IWindowService
    {
        EditJobInfoView ShowEditJobInfo(JobInfo jobInfo, IJobInfoChangedNotifier notifier);
        GalaPromptView ShowGalaPrompt(string data);
        AutoGalaProcessSelectionView ShowProcessSelection();
        ErrorView ShowError(string data, IEnumerable<string>? failedRows = null, Window? owner = null);
        void UpdateGalaPrompt(string data, GalaPromptView galaPromptView);
    }
}
