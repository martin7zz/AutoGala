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
using static AutoGala.Common.UiNavigation;

namespace AutoGala.Services.Helper
{
    internal sealed class GalaNavigator : NavigatorBase
    {
        private IntPtr _spinHandle;
        private IntPtr _gridHandle;

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

            if (!CheckCorrectPosition(MainWindow, items))
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

            _gridHandle = FindDescendantByClass(MainWindow, "TStringGrid");

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
            _spinHandle = FindRowCountSpin(MainWindow);

            return WriteItemsCore(sections,
                s => new[] { 
                    s.X?.ToString(CultureInfo.InvariantCulture) ?? "",
                    s.Y?.ToString(CultureInfo.InvariantCulture) ?? ""
                });
        }

        public bool WriteItems(ObservableCollection<RebarItem> rebars)
        {
            _spinHandle = FindRowCountSpin(MainWindow);

            return WriteItemsCore(rebars,
                r => new[] {
                    r.Area?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.X?.ToString(CultureInfo.InvariantCulture) ?? "",
                    r.Y?.ToString(CultureInfo.InvariantCulture) ?? "" }
                );
        }

        public bool WriteItems(ObservableCollection<LoadItem> loads, bool isSimpleBending)
        {
            _spinHandle = FindRowCountSpin(MainWindow);

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
            var edit = FindDescendantByClass(MainWindow, "TInplaceEdit");

            if (edit == IntPtr.Zero)
            {
                Debug.WriteLine("No inplace editor found");
                return false;
            }

            if (!FocusWindow(edit, MainWindow))
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
            var edit = FindDescendantByClass(MainWindow, "TInplaceEdit");

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
        private bool SetRowCount(int target)
        {
            if (_spinHandle == IntPtr.Zero)
                return false;

            if (!FocusWindow(_spinHandle, MainWindow))
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
            return spins[1];
        }

        // uses keyboard state table to simulate ctrl + home so that it automatically sets the current cell to the first in the grid
        private void GoToFirstCell()
        {
            if (_gridHandle == IntPtr.Zero) return;

            uint targetThread = GetWindowThreadProcessId(_gridHandle, out _);
            uint currentThread = GetCurrentThreadId();
            bool attached = AttachThreadInput(currentThread, targetThread, true);

            try
            {
                FocusWindow(_gridHandle, MainWindow);

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

        // looks for ui elements to figure out which ui window is currently active
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
                    MainWindow,
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
    }
}