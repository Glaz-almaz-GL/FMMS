using Avalonia.Controls;
using Avalonia.Input;
using FMMS.ViewModels;
using Huskui.Avalonia.Controls;
using System.Linq;

namespace FMMS.Views // ������� ���� ������������ ���
{
    public partial class HomePage : Page // ��� UserControl, � ����������� �� ������ �������
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
                // �������� ����� ViewModel ��� ���������� SelectedFiles
                viewModel.UpdateSelectedItems(dataGrid.SelectedItems);
            }
        }

        private async void FilesDataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            // ���������, ������ �� Ctrl � C ������������
            e.ClipboardRowContent.Clear();

            // �������� ���������� �������� �� DataGrid
            // �����������, ��� DataGrid �������� � �������� ItemsSource,
            // � �� ����������� SelectionMode, ����������� ������������� �����
            if (DataContext is HomeViewModel viewModel && sender is DataGrid && viewModel.SelectedFiles.Any())
            {
                await viewModel.CopySelectedItemsAsync();
            }
        }

        // �����-���������� ��� DragOver
        private void OnDragOver(object? sender, DragEventArgs e)
        {
            // �������� ViewModel � �������� � �����
            if (DataContext is HomeViewModel)
            {
                HomeViewModel.OnDragOver(e); // �������� ����� � ViewModel
            }
        }

        // �����-���������� ��� Drop
        private void OnDrop(object? sender, DragEventArgs e)
        {
            // �������� ViewModel
            if (DataContext is HomeViewModel vm)
            {
                // �������� ����������� ����� �� ViewModel, �� �� ���� ��� �����.
                // Avalonia �� ���� ���������� async ����������� �������.
                _ = vm.OnDropAsync(e); // ���������� �������� ������������, ����� �������� �������������� CS4014
            }
        }
    }
}