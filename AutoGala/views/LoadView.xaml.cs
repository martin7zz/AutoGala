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

        private void SaveEditButton_PreviewMouseLeftButtonDown(
           object sender,
           MouseButtonEventArgs e)
        {
            var vm = (LoadViewModel)DataContext;
            if (vm.IsGridReadOnly) return;

            bool committed = LoadGrid.CommitEdit(DataGridEditingUnit.Row, true);

            if (!committed || Validation.GetHasError(LoadGrid))
            {
                MessageBox.Show("Fix invalid N/Mx/My values before saving.");
                e.Handled = true;
            }
        }
    }
}
