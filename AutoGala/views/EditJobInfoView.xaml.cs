using System.Windows;

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
