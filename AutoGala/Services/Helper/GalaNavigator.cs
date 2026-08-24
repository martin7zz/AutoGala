using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AutoGala.Services.Helper
{
    internal sealed class GalaNavigator
    {
        private const uint WM_SETTEXT = 0x000C;
        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_GETTEXTLENGTH = 0x000E;

        private const uint GW_CHILD = 5;
        private const uint GW_HWNDNEXT = 2;

        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;
        private const int VK_UP = 0x26;
        private const int VK_DOWN = 0x28;
        private const int VK_HOME = 0x24;
        private const int VK_CONTROL = 0x11;

        private IntPtr _mainWindow;
        private IntPtr _spinHandle;
        private IntPtr _gridHandle;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            string lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam);
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SendMessage(
            IntPtr hWnd,
            uint Msg,
            int wParam,
            StringBuilder lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(
            IntPtr hWnd,
            uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr hWndParent,
            EnumChildProc lpEnumFunc,
            IntPtr lParam);

        private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(
            IntPtr hWnd,
            StringBuilder lpClassName,
            int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);


        [DllImport("user32.dll")]
        private static extern bool PostMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern bool SetKeyboardState(byte[] lpKeyState);


        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        public bool Attach(AutomationElement clickedElement)
        {
            var window = GetTopLevelWindow(clickedElement);
            if (window == null) return false;

            _mainWindow = (IntPtr)window.Current.NativeWindowHandle;

            //Debug.WriteLine($"Main : 0x{_mainWindow:X}");
            //Debug.WriteLine($"Spin : 0x{_spinHandle:X}");
            //Debug.WriteLine($"Grid : 0x{_gridHandleLoads:X}");

            return _mainWindow != IntPtr.Zero;
        }

        private bool CheckCorrectPosition<T>(IntPtr mainWindow, ObservableCollection<T> items)
        {
            var rebar = IntPtr.Zero;
            //var rebar = FindDescendantByTextAndClassName(mainWindow, "Bar ratio", "TRadioGroup");
            var reinfLayout = FindDescendantByTextAndClassName(mainWindow, "TS_reinf_layout", "TTabSheet");

            int reinfLayoutCount = GetDescendantsCount(reinfLayout);

            if (reinfLayout != IntPtr.Zero && reinfLayoutCount >= 5)
            {
                var panels = FindDescendantsByClass(
                    reinfLayout,
                    "TPanel");

                if (panels.Count > 1)
                {
                    rebar = panels[1];
                }
            }

            var load = FindDescendantByTextAndClassName(mainWindow, "TS_stability", "TTabSheet");

            var slsDataButton = FindDescendantByTextAndClassName(mainWindow, "SLS data", "TBitBtn");

            if (reinfLayout != IntPtr.Zero && typeof(T) == typeof(SectionItem) && rebar == IntPtr.Zero && reinfLayoutCount < 5)
            {
                return true;
            }

            if (rebar != IntPtr.Zero && typeof(T) == typeof(RebarItem) && reinfLayout != IntPtr.Zero && IsWindowVisible(rebar))
            {
                return true;
            }

            if (slsDataButton != IntPtr.Zero && typeof(T) == typeof(LoadItem) && IsWindowVisible(slsDataButton) && rebar == IntPtr.Zero && reinfLayout == IntPtr.Zero)
            {
                var pageControls = FindDescendantsByClass(
                    _mainWindow,
                    "TPageControl");

                if (pageControls.Count > 3)
                {
                    load = pageControls[3];
                }

                if (load != IntPtr.Zero)
                {
                    {
                        return true;
                    }
                }
            }

            if (load != IntPtr.Zero && typeof(T) == typeof(LoadItem))
            {
                return true;
            }

            return false;
        }

        // FOR TESTING
        private static void DumpWindows(IntPtr parent, int depth = 0)
        {
            EnumChildWindows(parent, (hwnd, _) =>
            {
                var sb = new StringBuilder(128);
                GetClassName(hwnd, sb, sb.Capacity);

                Debug.WriteLine($"{new string(' ', depth * 2)}0x{hwnd:X}  {sb}");

                DumpWindows(hwnd, depth + 1);

                return true;
            }, IntPtr.Zero);
        }

        // FOR TESTING
        void DumpRaw(AutomationElement element, int depth = 0)
        {
            var rect = element.Current.BoundingRectangle;
            Debug.WriteLine(
                $"{new string(' ', depth * 2)}" +
                $"{element.Current.ControlType.ProgrammaticName} - '{element.Current.Name}'" +
                $" offscreen={element.Current.IsOffscreen} rect={rect}");

            var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach (AutomationElement child in children)
                DumpRaw(child, depth + 1);
        }

        
        /// Pushes the ObservableCollection into Gala: sets the row count to
        /// match, waits for the row edits to appear, then writes each
        /// item into the corresponding row's cells.

        private bool WriteItemsCore<T>(
            ObservableCollection<T> items,
            Func<T, IEnumerable<string>> fieldSelector)
        {
            if (_spinHandle == IntPtr.Zero)
            {
                return false;
            }

            if (!CheckCorrectPosition(_mainWindow, items))
            {
                throw new InvalidOperationException(
                     $"Target page must be: {(typeof(T) == typeof(SectionItem) ? "Section"
                         : typeof(T) == typeof(RebarItem) ? "Rebar"
                         : typeof(T) == typeof(LoadItem) ? "Loads"
                         : typeof(T).Name)}");
            }

            if (!SetRowCount(items.Count))
            {
                return false;
            }

            _gridHandle = FindDescendantByClass(_mainWindow, "TStringGrid");

            if (_gridHandle == IntPtr.Zero)
            {
                return false;
            }

            int spinVlaue = GetCurrentSpinValue();

            //FocusWindow(_gridHandle);

            GoToFirstCell();

            foreach (var item in items)
            {
                foreach (var value in fieldSelector(item))
                {
                    if (!WriteCurrentCell(value))
                    {
                        return false;
                    }
                    if(!NextCell())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool WriteItems(ObservableCollection<SectionItem> sections)
        {
            _spinHandle = FindRowCountSpin(_mainWindow);

            return WriteItemsCore(sections,
                s => new[] { 
                    s.X?.ToString(CultureInfo.InvariantCulture) ?? "",
                    s.Y?.ToString(CultureInfo.InvariantCulture) ?? ""
                });
        }

        public bool WriteItems(ObservableCollection<RebarItem> rebars)
        {
            _spinHandle = FindRowCountSpin(_mainWindow);

            return WriteItemsCore(rebars,
                r => new[] {
                    r.Area?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.X?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.Y?.ToString(CultureInfo.InvariantCulture) ?? "" }
                );
        }

        public bool WriteItems(ObservableCollection<LoadItem> loads, bool isSimpleBending)
        {
            _spinHandle = FindRowCountSpin(_mainWindow);

            return WriteItemsCore(loads,
                l => isSimpleBending 
                ? new[] 
                { 
                    l.N?.ToString(CultureInfo.InvariantCulture) ?? "",
                    l.Mx?.ToString(CultureInfo.InvariantCulture) ?? "" 
                }
                : new[] 
                {
                    l.N?.ToString(CultureInfo.InvariantCulture) ?? "",
                    l.Mx?.ToString(CultureInfo.InvariantCulture) ?? "",
                    l.My?.ToString(CultureInfo.InvariantCulture) ?? ""
                }
            );
        }

        private bool WriteCurrentCell(string value)
        {
            var edit = FindDescendantByClass(_mainWindow, "TInplaceEdit");

            if (edit == IntPtr.Zero)
            {
                Debug.WriteLine("No inplace editor found");
                return false;
            }

            if (!FocusWindow(edit))
            {
                Debug.WriteLine("Failed to focus inplace editor.");
                return false;
            }

            IntPtr result = SendMessage(
                edit,
                WM_SETTEXT,
                IntPtr.Zero,
                value);

            if (result == IntPtr.Zero)
            {
                Debug.WriteLine(
                    $"WM_SETTEXT failed for '{value}'.");

                return false;
            }

            return true;
        }

        private bool NextCell()
        {
            var edit = FindDescendantByClass(_mainWindow, "TInplaceEdit");

            if (edit == IntPtr.Zero)
            {
                Debug.WriteLine("NextCell: editor not found.");
                return false;
            }

            SendMessage(
                edit,
                WM_KEYDOWN,
                (IntPtr)Keys.Tab,
                IntPtr.Zero);

            SendMessage(
                edit,
                WM_KEYUP,
                (IntPtr)Keys.Tab,
                IntPtr.Zero);

            return true;
        }

        // row count management

        private bool FocusWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return false;

            uint targetThread = GetWindowThreadProcessId(hwnd, out _);
            uint currentThread = GetCurrentThreadId();

            bool attached = false;

            try
            {
                if (targetThread != currentThread)
                {
                    attached = AttachThreadInput(
                        currentThread,
                        targetThread,
                        true);

                    //Debug.WriteLine($"Attach: {attached}");

                    if (!attached)
                        return false;
                }

                SetForegroundWindow(_mainWindow);

                IntPtr previous = SetFocus(hwnd);
                IntPtr actual = GetFocus();

                //Debug.WriteLine($"Target:   0x{hwnd:X}");
                //Debug.WriteLine($"Previous: 0x{previous:X}");
                //Debug.WriteLine($"Actual:   0x{actual:X}");

                return actual == hwnd;
            }
            finally
            {
                if (attached)
                    AttachThreadInput(
                        currentThread,
                        targetThread,
                        false);
            }
        }

        private bool SetRowCount(int target)
        {
            if (_spinHandle == IntPtr.Zero)
                return false;

            if (!FocusWindow(_spinHandle))
            {
                Debug.WriteLine("SetRowCount: failed to focus spin.");
                return false;
            }

            int current = GetCurrentSpinValue();

            //if (current == target)
            //{
            //    return true;
            //}

            int key = target > current
                ? VK_UP
                : VK_DOWN;

            while (current != target)
            {
                SendMessage(
                    _spinHandle,
                    WM_KEYDOWN,
                    (IntPtr)key,
                    IntPtr.Zero);
                SendMessage(
                    _spinHandle,
                    WM_KEYUP,
                    (IntPtr)key,
                    IntPtr.Zero);

                current += target > current ? 1 : -1;
            }

            int actual = GetCurrentSpinValue();

            if (actual != target)
            {
                Debug.WriteLine($"SetRowCount: expected {target}, got {GetCurrentSpinValue()}");
                return false;
            }

            return true;
        }

        // internals

        // uses keyboard state table to simulate ctrl + home so that it automatically sets the current cell to the first in the grid
        private void GoToFirstCell()
        {
            if (_gridHandle == IntPtr.Zero) return;

            uint targetThread = GetWindowThreadProcessId(_gridHandle, out _);
            uint currentThread = GetCurrentThreadId();
            bool attached = AttachThreadInput(currentThread, targetThread, true);

            try
            {
                FocusWindow(_gridHandle);

                byte[] keyState = new byte[256];
                GetKeyboardState(keyState);
                keyState[VK_CONTROL] = 0x80; // high bit = "down"
                SetKeyboardState(keyState);

                SendMessage(_gridHandle, WM_KEYDOWN, (IntPtr)VK_HOME, IntPtr.Zero);
                SendMessage(_gridHandle, WM_KEYUP, (IntPtr)VK_HOME, IntPtr.Zero);

                keyState[VK_CONTROL] = 0x00;
                SetKeyboardState(keyState);
            }
            finally
            {
                if (attached)
                    AttachThreadInput(currentThread, targetThread, false);
            }
        }

        private int GetCurrentSpinValue()
        {
            int len = SendMessage(_spinHandle, WM_GETTEXTLENGTH, 0, null);
            var sb = new StringBuilder(len + 1);
            SendMessage(_spinHandle, WM_GETTEXT, sb.Capacity, sb);
            return int.TryParse(sb.ToString(), out var v) ? v : 0;
        }

        private static AutomationElement? GetTopLevelWindow(AutomationElement el)
        {
            var walker = TreeWalker.ControlViewWalker;
            var current = el;

            while (current != null && current.Current.ControlType != ControlType.Window)
            {
                var parent = walker.GetParent(current);
                if (parent == null) break;
                current = parent;
            }

            return current;
        }

        private static IntPtr FindDescendantByTextAndClassName(IntPtr parent, string text, string className)
        {
            IntPtr result = IntPtr.Zero;

            EnumChildWindows(parent, (hwnd, _) =>
            {
                var textBuffer = new StringBuilder(256);
                var classBuffer = new StringBuilder(128);

                GetWindowText(hwnd, textBuffer, textBuffer.Capacity);
                GetClassName(hwnd, classBuffer, classBuffer.Capacity);

                string text1 = textBuffer.ToString();
                string text2 = classBuffer.ToString();

                //Debug.WriteLine($"{text2} {text1}");

                if (textBuffer.ToString().Equals(text, StringComparison.OrdinalIgnoreCase) &&
                    classBuffer.ToString().Equals(className, StringComparison.OrdinalIgnoreCase) && IsWindowVisible(hwnd))
                {
                    result = hwnd;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        private static IntPtr FindDescendantByClass(IntPtr parent, string className)
        {
            IntPtr result = IntPtr.Zero;

            EnumChildWindows(parent, (hwnd, _) =>
            {
                var sb = new StringBuilder(128);
                GetClassName(hwnd, sb, sb.Capacity);

                if (sb.ToString().Equals(className, StringComparison.OrdinalIgnoreCase) && IsWindowVisible(hwnd))
                {
                    result = hwnd;
                    return false; // stop enumeration
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        private static List<IntPtr> FindDescendantsByClass(IntPtr parent, string className)
        {
            var results = new List<IntPtr>();

            EnumChildWindows(parent, (hwnd, _) =>
            {
                var sb = new StringBuilder(128);
                GetClassName(hwnd, sb, sb.Capacity);

                if (sb.ToString().Equals(className, StringComparison.OrdinalIgnoreCase) && IsWindowVisible(hwnd))
                {
                    results.Add(hwnd);
                }

                return true;
            }, IntPtr.Zero);

            return results;
        }

        private static int GetDescendantsCount(IntPtr parent)
        {
            int count = 0;

            for (IntPtr hwnd = GetWindow(parent, GW_CHILD);
                 hwnd != IntPtr.Zero;
                 hwnd = GetWindow(hwnd, GW_HWNDNEXT))
            {
                if (IsWindowVisible(hwnd))
                {
                    count++;
                }
            }

            return count;
        }

        private static IntPtr FindRowCountSpin(IntPtr mainWindow)
        {
            var spins = FindDescendantsByClass(
                mainWindow,
                "TRxSpinEdit");

            if (spins.Count == 0)
            {
                Debug.WriteLine("No TRxSpinEdit controls found.");
                return IntPtr.Zero;
            }

            if (spins.Count == 1)
                return spins[0];

            // Gala-specific assumption:
            // second TRxSpinEdit is the row-count control.
            return spins[1];
        }
    }
}