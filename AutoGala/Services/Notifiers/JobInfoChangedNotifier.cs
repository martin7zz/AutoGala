using AutoGala.Contracts;

namespace AutoGala.Services.Notifiers
{
    public class JobInfoChangedNotifier : IJobInfoChangedNotifier
    {
        public event Action? JobInfoChanged;

        public void NotifyJobInfoChanged() => JobInfoChanged?.Invoke();
    }
}
