using AutoGala.ViewModels;
using Plugin.Core.Models;
using System.Windows.Controls;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for LoadView.xaml
    /// </summary>
    public partial class LoadView : UserControl
    {
        public LoadView()
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

        private void LoadGrid_ValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (DataContext is LoadViewModel vm)
            {
                vm.RegisterValidationError(e.Action == ValidationErrorEventAction.Added);
            }
        }

        private void LoadGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is LoadViewModel vm)
            {
                foreach (LoadItem item in e.RemovedItems)
                {
                    vm.SelectedLoads.Remove(item);
                }

                foreach (LoadItem item in e.AddedItems)
                {
                    vm.SelectedLoads.Add(item);
                }
            }
        }
    }
}
