using System.Windows;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for ClipboardErrorView.xaml
    /// </summary>
    public partial class ClipboardErrorView : Window
    {
        public ClipboardErrorView()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
