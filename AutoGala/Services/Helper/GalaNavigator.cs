using Plugin.Core.Contracts;
using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using static AutoGala.Common.UiNavigation;

namespace AutoGala.Services.Helper
{
    internal sealed class GalaNavigator : NavigatorBase
    {
        private IntPtr _spinHandle;
        private IntPtr _gridHandle;

        private IntPtr _editHandle;

        private IntPtr GetEditHandle(bool forceRefresh = false)
        {
            if (!forceRefresh && _editHandle != IntPtr.Zero && IsWindowVisible(_editHandle))
                return _editHandle;

            return _editHandle = FindInvisibleDescendantByClass(MainWindow, "TInplaceEdit");
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

            //_gridHandle = FindDescendantByClass(MainWindow, "TStringGrid");

            if (_gridHandle == IntPtr.Zero)
            {
                return false;
            }

            int spinVlaue = GetCurrentSpinValue();

            //FocusWindow(_gridHandle);

            //ShowWindow(_gridHandle, SW_HIDE);

            try
            {
                GoToFirstCell();
                GetEditHandle();

                foreach (var item in items)
                {
                    foreach (var value in fieldSelector(item))
                    {
                        if (!WriteCurrentCell(value))
                        {
                            return false;
                        }

                        if (!NextCell())
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            finally
            {
                ShowWindow(_gridHandle, SW_SHOW);
            }
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
            if (_editHandle == IntPtr.Zero)
            {
                Debug.WriteLine("No inplace editor found");
                return false;
            }

            //if (!FocusWindow(_editHandle, MainWindow))
            //{
            //    Debug.WriteLine("Failed to focus inplace editor.");
            //    return false;
            //}

            IntPtr result = SendMessage(
                _editHandle,
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
            if (_editHandle == IntPtr.Zero)
            {
                Debug.WriteLine("NextCell: editor not found.");
                return false;
            }

            SendMessage(
                _editHandle,
                WM_KEYDOWN,
                (IntPtr)Keys.Tab,
                IntPtr.Zero);

            SendMessage(
                _editHandle,
                WM_KEYUP,
                (IntPtr)Keys.Tab,
                IntPtr.Zero);

            _editHandle = GetEditHandle(forceRefresh: true);

            return _editHandle != IntPtr.Zero;
        }

        private bool ReadItemsCore<T>(
            int rowCount,
            int columnCount,
            Func<string[], T> itemFactory,
            out ObservableCollection<T> items)
        {
            items = new ObservableCollection<T>();

            if (!CheckCorrectPosition(MainWindow, items))
            {
                throw new InvalidOperationException(
                     $"Target page must be: {(typeof(T) == typeof(SectionItem) ? "Section"
                         : typeof(T) == typeof(RebarItem) ? "Rebar"
                         : typeof(T) == typeof(LoadItem) ? "Loads"
                         : typeof(T).Name)}");
            }

            _gridHandle = FindDescendantByClass(MainWindow, "TStringGrid");

            if (_gridHandle == IntPtr.Zero)
            {
                return false;
            }

            ShowWindow(_gridHandle, SW_HIDE);

            try
            {
                GoToFirstCell();

                GetEditHandle(forceRefresh: true);

                for (int row = 0; row < rowCount; row++)
                {
                    var values = new string[columnCount];

                    for (int column = 0; column < columnCount; column++)
                    {
                        var value = ReadCurrentCell();

                        if (value == null)
                            return false;

                        values[column] = value;

                        if (!(row == rowCount - 1 && column == columnCount - 1))
                        {
                            if (!NextCell())
                                return false;
                        }
                    }

                    items.Add(itemFactory(values));
                }
                return true;
            }
            finally
            {
                ShowWindow(_gridHandle, SW_SHOW);
            }
        }

        public bool ReadItems(ISectionService sectionService, out ObservableCollection<SectionItem> sections)
        {
            _spinHandle = FindRowCountSpin(MainWindow);

            if (_spinHandle == IntPtr.Zero)
            {
                sections = new ObservableCollection<SectionItem>();
                return false;
            }

            int rowCount = GetCurrentSpinValue();

            int id = 1;

            return ReadItemsCore(
                rowCount,
                2,
                values =>
                {
                    var section = sectionService.CreateSection(ParseNullableDouble(values[0]), ParseNullableDouble(values[1]));

                    section.Id = id++;

                    return section;
                },
                out sections);
        }

        public bool ReadItems(IRebarService rebarService, out ObservableCollection<RebarItem> rebars)
        {
            _spinHandle = FindRowCountSpin(MainWindow);

            if (_spinHandle == IntPtr.Zero)
            {
                rebars = new ObservableCollection<RebarItem>();
                return false;
            }

            int rowCount = GetCurrentSpinValue();

            int id = 1;

            return ReadItemsCore(
                rowCount,
                3,
                values =>
                {
                    var rebar = rebarService.CreateRebar(
                        ParseNullableDouble(values[0]),
                        ParseNullableDouble(values[1]),
                        ParseNullableDouble(values[2]));

                    rebar.Id = id++;

                    return rebar;
                },
                out rebars);
        }

        public bool ReadItems(
            ILoadService loadService,
            bool isSimpleBending,
            out ObservableCollection<LoadItem> loads)
        {
            _spinHandle = FindRowCountSpin(MainWindow);

            if (_spinHandle == IntPtr.Zero)
            {
                loads = new ObservableCollection<LoadItem>();
                return false;
            }

            int rowCount = GetCurrentSpinValue();

            int columnCount = isSimpleBending ? 2 : 3;

            int id = 1;

            return ReadItemsCore(
                rowCount,
                columnCount,
                values =>
                {
                    var load = loadService.CreateLoad(ParseNullableDouble(values[0]),
                        ParseNullableDouble(values[1]), 0);

                    if (!isSimpleBending)
                        load.My = ParseNullableDouble(values[2]);


                    load.Id = id++;
                    return load;
                },
                out loads);
        }

        private static double? ParseNullableDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : null;
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

            for (int i = 0; i < Math.Abs(target - current); i++)
            {
                if (i == 1)
                {
                    _gridHandle = FindDescendantByClass(MainWindow, "TStringGrid");

                    ShowWindow(_gridHandle, SW_HIDE);
                }
                SendMessage(_spinHandle, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
                SendMessage(_spinHandle, WM_KEYUP, (IntPtr)key, IntPtr.Zero);
            }

            int actual = GetCurrentSpinValue();

            if (_gridHandle == IntPtr.Zero)
            {
                _gridHandle = FindDescendantByClass(MainWindow, "TStringGrid");

                ShowWindow(_gridHandle, SW_HIDE);
            }

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

        private string? ReadCurrentCell()
        {
            if (_editHandle == IntPtr.Zero)
            {
                Debug.WriteLine("No inplace editor found.");
                return null;
            }

            int len = SendMessage(
                _editHandle,
                WM_GETTEXTLENGTH,
                0,
                null);

            var sb = new StringBuilder(len + 1);

            SendMessage(
                _editHandle,
                WM_GETTEXT,
                sb.Capacity,
                sb);

            return sb.ToString();
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