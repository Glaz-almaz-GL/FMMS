using FMMS.ViewModels;
using Huskui.Avalonia.Controls;

namespace FMMS.Views;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}