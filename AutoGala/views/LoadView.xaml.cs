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

        //private void LoadGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.V &&
        //        Keyboard.Modifiers == ModifierKeys.Control)
        //    {
        //        PasteFromExcel();
        //        e.Handled = true;
        //    }
        //}

        //private void PasteFromExcel()
        //{
        //    if (DataContext is not LoadViewModel vm)
        //    {
        //        return;
        //    }

        //    var clipboard = Clipboard.GetText();

        //    if (string.IsNullOrWhiteSpace(clipboard))
        //    {
        //        return;
        //    }

        //    var rows = clipboard.Split(
        //        new[] { "\r\n", "\n" },
        //        StringSplitOptions.RemoveEmptyEntries);

        //    foreach (var row in rows) 
        //    {
        //        var cells = row.Split('\t');

        //        if (cells.Length < 3)
        //        {
        //            continue;
        //        }

        //        if (double.TryParse(cells[0], out var n) &&
        //            double.TryParse(cells[1], out var mx) &&
        //            double.TryParse(cells[2], out var my))
        //        {
        //            var load = new LoadItem
        //            {
        //                N = n,
        //                Mx = mx,
        //                My = my
        //            };

        //            vm.Loads.Add(load);
        //        }
        //    }
        //}
    }
}
