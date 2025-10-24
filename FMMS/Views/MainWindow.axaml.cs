using FMMS.Managers;
using FMMS.Models;
using FMMS.ViewModels;
using Huskui.Avalonia.Controls;

namespace FMMS.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        SettingsManager.LoadSettings();
        GrowlsManager.Initialize(this);
        DialogsManager.Initialize(this, this);
        ClipboardManager.Initialize(this);

        DataContext = new MainViewModel();

        if (SettingsManager.CurrentSettings.AutoCheckForUpdates)
        {
            _ = UpdateManager.CheckForUpdatesAsync();
        }
    }
}
