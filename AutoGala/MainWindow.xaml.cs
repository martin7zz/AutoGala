using AutoGala.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace AutoGala
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                e.Cancel = !vm.ConfirmClose();
            }
        }
    }
}