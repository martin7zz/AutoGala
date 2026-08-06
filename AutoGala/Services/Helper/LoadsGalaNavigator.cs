using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using System.Xml.Linq;

namespace AutoGala.Services.Helper
{
    internal sealed class LoadsGalaNavigator
    {
        private const string RootPaneName = "ts_solve";
        private const int RowsTabIndex = 2; // 0-indexed; confirm with Inspect
        private const int ColumnsPerRow = 3; // N, Mx, My

        private AutomationElement? _rootPane;
        private AutomationElement? _countEdit;
        private AutomationElement? _rowsPane;

        public bool Attach(AutomationElement clickedElement)
        {
            var window = GetTopLevelWindow(clickedElement);
            if (window == null) return false;

            _rootPane = FindDescendantByNameAndType(window, RootPaneName, ControlType.Pane);
            if (_rootPane == null) return false;

            var mainPane = FindChildByTypeIndex(_rootPane, ControlType.Pane, 2);
            if (mainPane == null) return false;

            _countEdit = FindChildByTypeIndex(mainPane, ControlType.Document, 0);
            if (_countEdit == null) return false;

            var loadsMainPane = FindChildByTypeIndex(mainPane, ControlType.Pane, 1);
            if (loadsMainPane == null) return false;

            _rowsPane = FindChildByTypeIndex(loadsMainPane, ControlType.Pane, 0);

            DumpRaw(_rootPane);
            DumpRaw(_rowsPane);

            return _countEdit != null && _rowsPane != null;
        }

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

        /// <summary>
        /// Pushes the ObservableCollection into Gala: sets the row count to
        /// match, waits for the row edits to appear, then writes each
        /// LoadItem's N/Mx/My into the corresponding row's cells.
        /// </summary>
        public bool WriteLoads(ObservableCollection<LoadItem> loads)
        {
            if (_countEdit == null || _rowsPane == null) return false;

            SetRowCount(loads.Count - 1);

            var firstCell = FindChildByTypeIndex(_rowsPane, ControlType.Document, 0);

            var all = _rowsPane.FindAll(TreeScope.Children, Condition.TrueCondition);

            if (firstCell == null)
                return false;

            firstCell.SetFocus();

            Thread.Sleep(100);

            var active = AutomationElement.FocusedElement;

            Debug.WriteLine(
                $"{active.Current.ControlType.ProgrammaticName} '{active.Current.Name}'");

            foreach (var load in loads)
            {
                WriteCurrentCell(load.N.ToString());
                NextCell();

                WriteCurrentCell(load.Mx.ToString());
                NextCell();

                WriteCurrentCell(load.My.ToString());
                NextRow();
            }

            return true;
        }

        private static void WriteCurrentCell(string value)
        {
            var focused = AutomationElement.FocusedElement;

            if (focused == null)
                return;

            foreach (var pattern in focused.GetSupportedPatterns())
            {
                Debug.WriteLine(pattern.ProgrammaticName);
            }

            SetValue(focused, value);
        }

        private static void NextCell()
        {
            System.Windows.Forms.SendKeys.SendWait("{TAB}");
            Thread.Sleep(100);
        }

        private static void NextRow()
        {
            System.Windows.Forms.SendKeys.SendWait("{TAB}");

            Thread.Sleep(100);
        }

        // --- row count management ---

        private void SetRowCount(int count)
        {
            //SetValue(_countEdit!, count.ToString());

            // Legacy MFC/VB apps often only react to focus-loss/Enter, not
            // the raw value change. If rows don't appear, try adding:
            // SendKeys.SendWait("{TAB}");  // needs System.Windows.Forms

            _countEdit.SetFocus();

            for (int i = 0; i < count; i++)
            {
                System.Windows.Forms.SendKeys.SendWait("{UP}");
                Thread.Sleep(50);
            }

            WaitUntil(() => GetRowElements().Count / ColumnsPerRow == Math.Max(count, 0), 3000);
        }

        // --- internals ---

        private System.Collections.Generic.List<System.Collections.Generic.List<AutomationElement>> GetGroupedRows()
        {
            var cells = GetRowElements();

            return cells
                .GroupBy(c => Math.Round(c.Current.BoundingRectangle.Y / 5.0) * 5.0)
                .OrderBy(g => g.Key)
                .Select(g => g.OrderBy(c => c.Current.BoundingRectangle.X).ToList())
                .ToList();
        }

        private System.Collections.Generic.List<AutomationElement> GetRowElements()
        {
            if (_rowsPane == null) return new();

            var edits = _rowsPane.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document));

            return edits.Cast<AutomationElement>().ToList();
        }

        private static void SetValue(AutomationElement el, string value)
        {
            if (el.TryGetCurrentPattern(ValuePattern.Pattern, out var p))
            {
                var vp = (ValuePattern)p;
                if (!vp.Current.IsReadOnly)
                    vp.SetValue(value);
            }
            System.Windows.Forms.SendKeys.SendWait(value);
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

        private static AutomationElement? FindDescendantByNameAndType(
            AutomationElement scope, string name, ControlType type)
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, type),
                new PropertyCondition(AutomationElement.NameProperty, name));

            return scope.FindFirst(TreeScope.Descendants, condition);
        }

        private static AutomationElement? FindFirstDescendant(AutomationElement scope, ControlType type) =>
            scope.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, type));

        private static AutomationElement? FindChildByTypeIndex(AutomationElement scope, ControlType type, int index)
        {
            var matches = scope.FindAll(TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, type));

            return index >= 0 && index < matches.Count ? matches[index] : null;
        }

        private static void WaitUntil(Func<bool> condition, int timeoutMs)
        {
            var elapsed = 0;
            const int pollMs = 100;
            while (!condition() && elapsed < timeoutMs)
            {
                Thread.Sleep(pollMs);
                elapsed += pollMs;
            }
        }
    }
}