using Autodesk.AutoCAD.Interop;
using AutoGala.Contracts;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AutoGala.Services
{
    public class AutoGalaProcessService : IAutoGalaProcessService
    {
        public AcadApplication? GetAcadApplicationByProcessId(int pid)
        {
            int hr = GetRunningObjectTable(0, out IRunningObjectTable rot);
            Marshal.ThrowExceptionForHR(hr);

            rot.EnumRunning(out IEnumMoniker enumMoniker);
            CreateBindCtx(0, out IBindCtx bindCtx);

            try
            {
                enumMoniker.Reset();

                IMoniker[] monikers = new IMoniker[1];

                while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    var moniker = monikers[0];

                    try
                    {
                        moniker.GetDisplayName(
                            bindCtx,
                            null,
                            out string displayName);

                        Debug.WriteLine($"ROT: {displayName}");

                        if (rot.GetObject(moniker, out object obj) != 0)
                            continue;

                        try
                        {
                            if (obj is not AcadDocument doc)
                                continue;

                            Debug.WriteLine($"DOCUMENT: {doc.Name}");

                            AcadApplication? app = null;

                            try
                            {
                                app = doc.Application;

                                GetWindowThreadProcessId(
                                    (IntPtr)app.HWND,
                                    out uint appPid);

                                Debug.WriteLine(
                                    $"APP PID: {appPid}, requested PID: {pid}");

                                if (appPid == pid)
                                {
                                    // Transfer ownership of app to caller.
                                    var result = app;
                                    app = null;

                                    return result;
                                }
                            }
                            finally
                            {
                                if (app != null)
                                    Marshal.ReleaseComObject(app);
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(obj);
                        }
                    }
                    catch (COMException ex)
                    {
                        Debug.WriteLine(
                            $"ROT entry failed: {ex.Message}");
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(moniker);
                    }
                }

                return null;
            }
            finally
            {
                Marshal.ReleaseComObject(bindCtx);
                Marshal.ReleaseComObject(enumMoniker);
                Marshal.ReleaseComObject(rot);
            }
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
