using AutoGala.ViewModels;
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
using System.Windows.Shapes;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for EditJobInfoView.xaml
    /// </summary>
    public partial class EditJobInfoView : Window
    {
        public EditJobInfoView()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
