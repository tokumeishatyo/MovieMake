using Microsoft.UI.Xaml.Controls;
using MovieMake.ViewModels;

namespace MovieMake.Views
{
    public sealed partial class ScriptEditorPage : Page
    {
        public ScriptEditorPage()
        {
            this.InitializeComponent();
            this.DataContext = new ScriptEditorViewModel();
            // Assign x:Name="RootPage" functionality? XAML compiler does it if x:Name is set in XAML tag?
            // Actually, Page tag needs x:Name="RootPage" for ElementName binding to work easily from DataTemplate.
        }
    }
}
