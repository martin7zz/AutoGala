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

            rot.EnumRunning(out IEnumMoniker enumMoniker);
            var monikers = new IMoniker[1];

            while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
            {
                CreateBindCtx(0, out IBindCtx bindCtx);
                monikers[0].GetDisplayName(bindCtx, null, out string name);

                if (!name.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (rot.GetObject(monikers[0], out object obj) != 0)
                {
                    continue; // object may have gone away between enum and bind
                }

                if (obj is AcadDocument doc)
                {
                    AcadApplication app = (AcadApplication)doc.Application;

                    GetWindowThreadProcessId((IntPtr)app.HWND, out uint appPid);
                    if (appPid == pid)
                    {
                        return app;
                    }
                }
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
