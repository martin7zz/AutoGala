using AutoGala.Contracts;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AutoGala.Services
{
    public class AutoGalaProcessService : IAutoGalaProcessService
    {
        public object? GetAcadApplicationByProcessId(int pid)
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
                        moniker.GetDisplayName(bindCtx, null, out string displayName);
                        Debug.WriteLine($"ROT: {displayName}");

                        if (rot.GetObject(moniker, out object obj) != 0)
                            continue;

                        object? app = null;
                        try
                        {
                            dynamic doc = obj;
                            // throws if obj isn't a document-like object
                            app = doc.Application;
                            long hwnd = Convert.ToInt64(((dynamic)app).HWND);
                            GetWindowThreadProcessId((IntPtr)hwnd, out uint appPid);

                            if (appPid == pid)
                            {
                                // This is the one COM object we hand back to the caller to hold
                                // onto (e.g. to poll Documents/ActiveDocument for button state).
                                // Don't release it — only the document (obj) and the moniker.
                                var result = app;
                                app = null;
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            // not an AutoCAD document, skip
                            Debug.WriteLine($"ROT entry failed: {ex.GetType().Name} 0x{ex.HResult:X8}: {ex.Message}");
                        }
                        finally
                        {
                            if (app != null) Marshal.ReleaseComObject(app);
                            Marshal.ReleaseComObject(obj);
                        }
                    }
                    catch (COMException ex)
                    {
                        Debug.WriteLine($"ROT entry failed: {ex.Message}");
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