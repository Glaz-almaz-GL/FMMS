using Avalonia.Controls;
using Avalonia.Input;
using FMMS.Models;
using FMMS.ViewModels;
using Huskui.Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace FMMS.Views;

public partial class DiskExplorerPage : Page
{
    public DiskExplorerPage()
    {
        DataContext = new DiskExplorerViewModel();
        InitializeComponent();
    }

    private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DiskExplorerViewModel viewModel && sender is DataGrid dataGrid)
        {
            viewModel.UpdateSelectedItems(dataGrid.SelectedItems);
        }
    }

    private async void DataGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is DiskExplorerViewModel viewModel && sender is DataGrid)
        {
            ObservableCollection<FolderInfo> selectedItems = viewModel.SelectedFolders;
            if (!selectedItems.Any())
            {
                return;
            }

            // Определяем комбинацию клавиш
            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
            {
                // Ctrl + C - ContextMenuCopyPathsAsync
                e.Handled = true;
                await viewModel.ContextMenuCopyPathsAsync();
            }
            else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.C)
            {
                // Ctrl + Shift + C - ContextMenuCopyPathsAsTsvAsync
                e.Handled = true;
                await viewModel.ContextMenuCopyPathsAsTsvAsync();
            }
            else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.O)
            {
                // Ctrl + O - ContextMenuOpenFolder
                e.Handled = true;
                viewModel.ContextMenuOpenFolder();
            }
            else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.O)
            {
                // Ctrl + Shift + O - ContextMenuOpenContainingFolder
                e.Handled = true;
                viewModel.ContextMenuOpenContainingFolder();
            }
        }
    }

    private async void FilesDataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
    {
        e.ClipboardRowContent.Clear();

        if (DataContext is HomeViewModel viewModel && sender is DataGrid && viewModel.SelectedFiles.Any())
        {
            await viewModel.CopySelectedItemsAsync();
        }
    }
}