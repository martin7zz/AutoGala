namespace AutoGala.Contracts
{
    public interface IJobInfoChangedNotifier
    {
        event Action? JobInfoChanged;
        void NotifyJobInfoChanged();
    }
}
