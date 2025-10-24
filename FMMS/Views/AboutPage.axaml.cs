using FMMS.ViewModels;
using Huskui.Avalonia.Controls;

namespace FMMS.Views;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        DataContext = new AboutViewModel();
    }
}