using AutoGala.ViewModels;
using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
