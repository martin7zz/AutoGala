using AutoGala.Services;
using AutoGala.ViewModels;
using AutoGala.views;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace AutoGala.Contracts
{
    public interface IWindowService
    {
        EditJobInfoView ShowEditJobInfo(JobInfo jobInfo, IJobInfoChangedNotifier notifier);
        GalaPromptView ShowGalaPrompt(string data);
        AutoGalaProcessSelectionView ShowProcessSelection(IAutoGalaProcessService autoGalaProcessService, ISortingService sortingService, ISectionsReceivedNotifier sectionsReceivedNotifier);
        ClipboardErrorView ShowClipboardError(string data, IEnumerable<string>? failedRows = null);
        void UpdateGalaPrompt(string data, GalaPromptView galaPromptView);
    }
}
