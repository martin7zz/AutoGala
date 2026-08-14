using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;
        private const int VK_UP = 0x26;
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

            _spinHandle = FindDescendantByClass(_mainWindow, "TRxSpinEdit");

            //Debug.WriteLine($"Main : 0x{_mainWindow:X}");
            //Debug.WriteLine($"Spin : 0x{_spinHandle:X}");
            //Debug.WriteLine($"Grid : 0x{_gridHandleLoads:X}");

            return _spinHandle != IntPtr.Zero;
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

            SetRowCount(items.Count);

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
                    WriteCurrentCell(value);
                    NextCell();
                }
            }

            return true;
        }

        public bool WriteItems(ObservableCollection<SectionItem> sections)
        {
            var spins = FindDescendantsByClass(_mainWindow, "TRxSpinEdit");

            _spinHandle = spins.Count > 1 ? spins[1] : IntPtr.Zero;
           
            return WriteItemsCore(sections,
                s => new[] { s.X.ToString(), s.Y.ToString() });
        }

        public bool WriteItems(ObservableCollection<RebarItem> rebars)
        {
            var spins = FindDescendantsByClass(_mainWindow, "TRxSpinEdit");

            _spinHandle = spins.Count > 1 ? spins[1] : IntPtr.Zero;

            return WriteItemsCore(rebars,
                r => new[] { r.Area.ToString(), r.X.ToString(), r.Y.ToString() });
        }

        public bool WriteItems(ObservableCollection<LoadItem> loads)
        {
            return WriteItemsCore(loads,
                l => new[] { l.N.ToString(), l.Mx.ToString(), l.My.ToString() });
        }

        private void WriteCurrentCell(string value)
        {
            var edit = FindDescendantByClass(_mainWindow, "TInplaceEdit");

            if (edit == IntPtr.Zero)
            {
                Debug.WriteLine("No inplace editor found");
                return;
            }
            
            FocusWindow(edit);

            SendMessage(
                edit,
                WM_SETTEXT,
                IntPtr.Zero,
                value);

        }

        private void NextCell()
        {
            var edit = FindDescendantByClass(_mainWindow, "TInplaceEdit");

            if (edit == IntPtr.Zero)
                return;

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

                    Debug.WriteLine($"Attach: {attached}");

                    if (!attached)
                        return false;
                }

                SetForegroundWindow(_mainWindow);

                IntPtr previous = SetFocus(hwnd);
                IntPtr actual = GetFocus();

                Debug.WriteLine($"Target:   0x{hwnd:X}");
                Debug.WriteLine($"Previous: 0x{previous:X}");
                Debug.WriteLine($"Actual:   0x{actual:X}");

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

        private void SetRowCount(int target)
        {
            if (_spinHandle == IntPtr.Zero)
                return;

            FocusWindow(_spinHandle);

            int current = GetCurrentSpinValue();

            while (current < target)
            {
                SendMessage(_spinHandle, WM_KEYDOWN, (IntPtr)VK_UP, IntPtr.Zero);
                SendMessage(_spinHandle, WM_KEYUP, (IntPtr)VK_UP, IntPtr.Zero);

                current++;
            }

            if (GetCurrentSpinValue() != target)
                Debug.WriteLine($"SetRowCount: expected {target}, got {GetCurrentSpinValue()}");
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

        private static IntPtr FindDescendantByClass(IntPtr parent, string className)
        {
            IntPtr result = IntPtr.Zero;

            EnumChildWindows(parent, (hwnd, _) =>
            {
                var sb = new StringBuilder(128);
                GetClassName(hwnd, sb, sb.Capacity);

                if (sb.ToString().Equals(className, StringComparison.OrdinalIgnoreCase))
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

                if (sb.ToString().Equals(className, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(hwnd);
                }

                return true;
            }, IntPtr.Zero);

            return results;
        }
    }
}