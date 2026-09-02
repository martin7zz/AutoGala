using AutoGala.ViewModels;
using Plugin.Core.Models;
using System.Windows.Controls;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for SectionView.xaml
    /// </summary>
    public partial class SectionView : UserControl
    {
        public SectionView()
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

        private void SectionGrid_ValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (DataContext is SectionViewModel vm)
            {
                vm.RegisterValidationError(e.Action == ValidationErrorEventAction.Added);
            }
        }

        private void SectionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is SectionViewModel vm)
            {
                foreach (SectionItem item in e.RemovedItems)
                {
                    vm.SelectedSections.Remove(item);
                }

                foreach (SectionItem item in e.AddedItems)
                {
                    vm.SelectedSections.Add(item);
                }
            }
        }
    }
}
