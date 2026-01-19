using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MovieMake.ViewModels;

namespace MovieMake.Views
{
    public sealed partial class SetupPage : Page
    {
        public SetupPage()
        {
            this.InitializeComponent();
            var vm = new SetupViewModel();
            this.DataContext = vm;
            
            // Listen for connection success to navigate
            vm.ConnectionSuccessful += Vm_ConnectionSuccessful;
        }

        private void Vm_ConnectionSuccessful(object? sender, System.EventArgs e)
        {
            // Navigate to ScriptEditorPage
            Frame.Navigate(typeof(ScriptEditorPage));
        }

        private void ApiPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SetupViewModel vm)
            {
                vm.ApiKey = ((PasswordBox)sender).Password;
            }
        }
    }
}
