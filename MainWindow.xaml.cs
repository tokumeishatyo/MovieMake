using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MovieMake
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Window does not have DataContext in WinUI 3, so we set it on the root element.
            RootGrid.DataContext = new MovieMake.ViewModels.MainViewModel();
        }

        private void ApiPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (RootGrid.DataContext is MovieMake.ViewModels.MainViewModel vm)
            {
                vm.ApiKey = ((PasswordBox)sender).Password;
            }
        }
    }
}
