using Microsoft.UI.Xaml;

namespace MovieMake
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            // Navigate to the SetupPage on startup
            RootFrame.Navigate(typeof(Views.SetupPage));
        }
    }
}
