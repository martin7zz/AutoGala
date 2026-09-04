using AutoGala.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for AutoGalaProcessSelectionView.xaml
    /// </summary>
    public partial class AutoGalaProcessSelectionView : Window
    {
        public AutoGalaProcessSelectionView()
        {
            InitializeComponent();
        }

        private void Instance_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && DataContext is AutoGalaProcessSelectionViewModel vm)
            {
                if (vm.SelectCommand.CanExecute(null))
                    vm.SelectCommand.Execute(null);
            }
        }
    }
}
