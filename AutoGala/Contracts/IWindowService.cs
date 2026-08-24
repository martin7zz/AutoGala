using AutoGala.ViewModels;
using AutoGala.views;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AutoGala.Contracts
{
    public interface IWindowService
    {
        EditJobInfoView ShowEditJobInfo(JobInfo jobInfo);
        GalaPromptView ShowGalaPrompt(string data);
        ClipboardErrorView ShowClipboardError(string data, IEnumerable<string>? failedRows = null);
        void UpdateGalaPrompt(string data, GalaPromptView galaPromptView);
    }
}
