using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Automation;
using static AutoGala.Common.UiNavigation;

namespace AutoGala.Services.Helper
{
    internal abstract class NavigatorBase
    {
        protected IntPtr MainWindow { get; private set; }

        public bool Attach(AutomationElement clickedElement)
        {
            var window = GetTopLevelWindow(clickedElement);

            if (window == null)
            {
                return false;
            }

            MainWindow = (IntPtr)window.Current.NativeWindowHandle;

            return MainWindow != IntPtr.Zero;
        }

        protected bool FocusWindow(IntPtr hwnd, IntPtr window)
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

                SetForegroundWindow(window);

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

        protected static string GetElementText(IntPtr el)
        {
            int len = SendMessage(el, WM_GETTEXT, 0, null);
            var sb = new StringBuilder(len + 1);
            SendMessage(el, WM_GETTEXT, sb.Capacity, sb);
            return sb.ToString();
        }

        protected static bool ElementContainsText(IntPtr el)
        {
            int len = SendMessage(el, WM_GETTEXT, 0, null);
            var sb = new StringBuilder(len + 1);
            SendMessage(el, WM_GETTEXT, sb.Capacity, sb);
            sb.ToString();

            return sb.Length > 0;
        }

        protected static AutomationElement? GetTopLevelWindow(AutomationElement el)
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

        protected static IntPtr FindDescendantByTextAndClassName(IntPtr parent, string text, string className)
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

        protected static IntPtr FindDescendantByClass(IntPtr parent, string className)
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

        protected static List<IntPtr> FindDescendantsByClass(IntPtr parent, string className)
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

        protected static int GetDescendantsCount(IntPtr parent)
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


        // FOR DEBUGGING
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

        // FOR DEBUGGING
        private static void DumpRaw(AutomationElement element, int depth = 0)
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
    }
}
