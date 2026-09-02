using Plugin.Core.Models;
using System.Diagnostics;
using static AutoGala.Common.UiNavigation;

namespace AutoGala.Services.Helper
{
    internal sealed class JobNavigator : NavigatorBase
    {
        private IntPtr _headerHandle;
        private IntPtr _jobNumberHandle;
        private IntPtr _jobTitleHandle;
        private IntPtr _clientHandle;
        private IntPtr _calcsHandle;
        private IntPtr _checkedByHandle;

        public bool WriteJob(JobInfo jobInfo)
        {
            if (!GetJobElements())
            {
                return false;
            }

            if (!ElementContainsText(_jobNumberHandle) || GetElementText(_jobNumberHandle) != jobInfo.JobNumber)
            {
                SendMessage(_jobNumberHandle, WM_SETTEXT, 0, jobInfo.JobNumber);
            }
            if (!ElementContainsText(_jobTitleHandle) || GetElementText(_jobTitleHandle) != jobInfo.JobTitle)
            {
                SendMessage(_jobTitleHandle, WM_SETTEXT, 0, jobInfo.JobTitle);
            }
            if (!ElementContainsText(_clientHandle) || GetElementText(_clientHandle) != jobInfo.Client)
            {
                SendMessage(_clientHandle, WM_SETTEXT, 0, jobInfo.Client);
            }
            if (!ElementContainsText(_calcsHandle) || GetElementText(_calcsHandle) != jobInfo.CalcsBy)
            {
                SendMessage(_calcsHandle, WM_SETTEXT, 0, jobInfo.CalcsBy);
            }
            if (!ElementContainsText(_checkedByHandle) || GetElementText(_checkedByHandle) != jobInfo.CheckedBy)
            {
                SendMessage(_checkedByHandle, WM_SETTEXT, 0, jobInfo.CheckedBy);
            }

            return true;
        }

        private bool GetJobElements()
        {
            _headerHandle = FindDescendantByTextAndClassName(MainWindow, "Header", "TTabSheet");

            if (_headerHandle == IntPtr.Zero)
            {
                Debug.WriteLine("header not found");
                return false;
            }

            var elements = FindDescendantsByClass(_headerHandle, "TEdit");

            _jobNumberHandle = elements[4];
            _jobTitleHandle = elements[3];
            _clientHandle = elements[2];
            _calcsHandle = elements[1];
            _checkedByHandle = elements[0];

            if (_jobNumberHandle == IntPtr.Zero
                || _jobTitleHandle == IntPtr.Zero
                || _clientHandle == IntPtr.Zero
                || _calcsHandle == IntPtr.Zero
                || _checkedByHandle == IntPtr.Zero)
            {
                Debug.WriteLine("Job element not found");
                return false;
            }

            return true;
        }
    }
}
