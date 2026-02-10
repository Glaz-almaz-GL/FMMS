using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FMMS.ViewModels;
using Huskui.Avalonia.Controls;

namespace FMMS.Views;

public partial class DiskExplorerPage : Page
{
    public DiskExplorerPage()
    {
        DataContext = new DiskExplorerViewModel();
        InitializeComponent();
    }
}