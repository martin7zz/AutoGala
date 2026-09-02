using AutoGala.views;
using Plugin.Core.Models;

namespace AutoGala.Contracts
{
    public interface IWindowService
    {
        EditJobInfoView ShowEditJobInfo(JobInfo jobInfo, IJobInfoChangedNotifier notifier);
        GalaPromptView ShowGalaPrompt(string data);
        AutoGalaProcessSelectionView ShowProcessSelection();
        ClipboardErrorView ShowClipboardError(string data, IEnumerable<string>? failedRows = null);
        void UpdateGalaPrompt(string data, GalaPromptView galaPromptView);
    }
}
