using Autodesk.AutoCAD.Interop;
using AutoGala.Contracts;
using DocumentFormat.OpenXml.Bibliography;
using Plugin.Core.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AutoGala.Services
{
    public class AutoGalaProcessService : IAutoGalaProcessService
    {
        public AcadApplication GetAcadApplicationByProcessId(int pid)
        {
            int hr = GetRunningObjectTable(0, out IRunningObjectTable rot);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);

            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);
            enumMoniker.Reset();

            CreateBindCtx(0, out IBindCtx bindCtx);

            try
            {
                IMoniker[] monikers = new IMoniker[1];

                while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    IMoniker moniker = monikers[0];

                    try
                    {
                        moniker.GetDisplayName(
                            bindCtx,
                            null,
                            out string displayName);

                        Debug.WriteLine($"ROT: {displayName}");

                        if (rot.GetObject(moniker, out object obj) != 0)
                            continue;

                        if (obj is AcadDocument doc)
                        {
                            AcadApplication app = doc.Application;

                            GetWindowThreadProcessId(
                                (IntPtr)app.HWND,
                                out uint appPid);

                            Debug.WriteLine(
                                $"  AcadDocument: {doc.Name}, PID: {appPid}");

                            if (appPid == pid)
                                return app;
                        }
                        else if (obj != null)
                        {
                            Marshal.ReleaseComObject(obj); // release bound but unused monikers
                        }
                    }
                    catch (COMException)
                    {
                        // ROT entries can disappear between enumeration and binding.
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(moniker);
                    }
                }
            }
            finally 
            {
                Marshal.ReleaseComObject(bindCtx);
                Marshal.ReleaseComObject(enumMoniker);
                Marshal.ReleaseComObject(rot);
            }
            

            return null;
        }

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(
            uint reserved,
            out IRunningObjectTable pprot);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(
            uint reserved,
            out IBindCtx ppbc);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint processId);
    }
}
