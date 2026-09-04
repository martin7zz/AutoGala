using System.Windows;
using System.Windows.Input;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for ClipboardErrorView.xaml
    /// </summary>
    public partial class ErrorView : Window
    {
        public ErrorView()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
