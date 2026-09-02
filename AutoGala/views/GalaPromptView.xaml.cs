using AutoGala.ViewModels;
using System.Windows;

namespace AutoGala.views
{
    /// <summary>
    /// Interaction logic for GalaPromptView.xaml
    /// </summary>
    public partial class GalaPromptView : Window
    {
        public GalaPromptView()
        {
            InitializeComponent();
        }

        public void SetMessage(string message)
        {
            ((GalaPromptViewModel)DataContext).Message = message;
        }
    }
}
