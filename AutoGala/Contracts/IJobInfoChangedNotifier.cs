using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGala.Contracts
{
    public interface IJobInfoChangedNotifier
    {
        event Action? JobInfoChanged;
        void NotifyJobInfoChanged();
    }
}
