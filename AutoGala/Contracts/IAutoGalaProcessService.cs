namespace AutoGala.Contracts
{
    public interface IAutoGalaProcessService
    {
        object? GetAcadApplicationByProcessId(int pid);
    }
}
