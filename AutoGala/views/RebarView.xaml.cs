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
    /// Interaction logic for RebarView.xaml
    /// </summary>
    public partial class RebarView : UserControl
    {
        public RebarView()
        {
            InitializeComponent();

            RebarGrid.ItemsSource = new List<RebarItem>
            {
                new RebarItem { Id = 1, Area = 5.5, X = 3, Y = 4},
                new RebarItem { Id = 2, Area = 6.2, X = 5, Y = 6 }
            };

        }
    }
}
