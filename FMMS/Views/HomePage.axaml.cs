using Avalonia.Controls;
using Avalonia.Input;
using FMMS.Items;
using FMMS.ViewModels;
using Huskui.Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace FMMS.Views // Укажите ваше пространство имён
{
    public partial class HomePage : Page // Или UserControl, в зависимости от вашего проекта
    {
        public HomePage()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();

            if (DragDropCard != null)
            {
                AddHandler(DragDrop.DragOverEvent, OnDragOver);
                AddHandler(DragDrop.DropEvent, OnDrop);
            }
        }

        private void DataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DataContext is HomeViewModel viewModel && sender is DataGrid dataGrid)
            {
                // Вызываем метод ViewModel для обновления SelectedFiles
                viewModel.UpdateSelectedItems(dataGrid.SelectedItems);
            }
        }

        private async void DataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is HomeViewModel viewModel && sender is DataGrid)
            {
                // Проверяем, есть ли выделенные элементы
                ObservableCollection<FileMetadata> selectedItems = viewModel.SelectedFiles;
                if (!selectedItems.Any())
                {
                    return;
                }

                // Определяем комбинацию клавиш
                if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.C)
                {
                    // Ctrl + C - CopySelectedItemsAsync
                    e.Handled = true;
                    await viewModel.CopySelectedItemsAsync();
                }
                else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.C)
                {
                    // Ctrl + Shift + C - CopyAsTsvAsync
                    e.Handled = true;
                    await viewModel.CopyAsTsvAsync();
                }
                else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.O)
                {
                    // Ctrl + O - OpenFile
                    e.Handled = true;
                    viewModel.OpenFile();
                }
                else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.O)
                {
                    // Ctrl + Shift + O - OpenContainingFolder
                    e.Handled = true;
                    viewModel.OpenContainingFolder();
                }
                else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.P)
                {
                    // Ctrl + P - ShowFileProperties
                    e.Handled = true;
                    viewModel.ShowFileProperties();
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

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (DataContext is HomeViewModel)
            {
                HomeViewModel.OnDragOver(e);
            }
        }

        // Метод-обработчик для Drop
        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (DataContext is HomeViewModel vm)
            {
                _ = vm.OnDropAsync(e);
            }
        }
    }
}