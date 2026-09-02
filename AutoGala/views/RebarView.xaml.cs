using AutoGala.ViewModels;
using Plugin.Core.Models;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for RebarView.xaml
    /// </summary>
    public partial class RebarView : UserControl
    {
        public RebarView()
        {
            InitializeComponent();
        }

        private void CopyingRowClipboardContent(
        object sender,
        DataGridRowClipboardEventArgs e)
        {
            if (e.ClipboardRowContent.Count > 0)
                e.ClipboardRowContent.RemoveAt(0);
        }

        private void RebarGrid_ValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (DataContext is RebarViewModel vm)
            {
                vm.RegisterValidationError(e.Action == ValidationErrorEventAction.Added);
            }
        }

        private void RebarGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is RebarViewModel vm)
            {
                foreach (RebarItem item in e.RemovedItems)
                {
                    vm.SelectedRebars.Remove(item);
                }
                foreach (RebarItem item in e.AddedItems)
                {
                    vm.SelectedRebars.Add(item);
                }
            }
        }

        private void RebarGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (DataContext is not RebarViewModel vm) return;

            Dispatcher.BeginInvoke(
                new Action(() => vm.CheckForDuplicates()),
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
    }
}
