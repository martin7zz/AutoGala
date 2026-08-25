using AutoGala.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Services
{
    public class JobInfoChangedNotifier : IJobInfoChangedNotifier
    {
        public event Action? JobInfoChanged;

        public void NotifyJobInfoChanged() => JobInfoChanged?.Invoke();
    }
}
