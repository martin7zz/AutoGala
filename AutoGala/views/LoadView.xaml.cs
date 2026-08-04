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
    /// Interaction logic for LoadView.xaml
    /// </summary>
    public partial class LoadView : UserControl
    {
        public LoadView()
        {
            InitializeComponent();

            LoadGrid.ItemsSource = new List<LoadItem>
            {
                new LoadItem {Id = 1, N = 100, Mx = 20, My = 30},
                new LoadItem { Id = 2, N = 120, Mx = 25, My = 35}
            };
        }
    }
}
