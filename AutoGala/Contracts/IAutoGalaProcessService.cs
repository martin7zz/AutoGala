using Autodesk.AutoCAD.Interop;

namespace AutoGala.Contracts
{
    public interface IAutoGalaProcessService
    {
        AcadApplication GetAcadApplicationByProcessId(int pid);
    }
}
