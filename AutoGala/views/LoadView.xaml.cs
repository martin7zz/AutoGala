using AutoGala.Services;
using AutoGala.ViewModels;
using Plugin.Core.Models;
using Plugin.Core.Services;
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

        private void LoadGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (DataContext is not LoadViewModel vm) return;

            if (vm.EditingLoad == null || !ReferenceEquals(e.Row.Item, vm.EditingLoad))
            {
                e.Cancel = true;
            }
        }

        private void LoadGrid_ValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (DataContext is LoadViewModel vm)
            {
                vm.RegisterValidationError(e.Action == ValidationErrorEventAction.Added);
            }
        }
    }
}
