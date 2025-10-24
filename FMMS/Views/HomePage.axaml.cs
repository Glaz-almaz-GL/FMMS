using Avalonia.Controls;
using Avalonia.Input;
using FMMS.ViewModels;
using Huskui.Avalonia.Controls;
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

        private async void FilesDataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            // Проверяем, нажаты ли Ctrl и C одновременно
            e.ClipboardRowContent.Clear();

            // Получаем выделенные элементы из DataGrid
            // Предположим, ваш DataGrid привязан к свойству ItemsSource,
            // и вы используете SelectionMode, позволяющий множественный выбор
            if (DataContext is HomeViewModel viewModel && sender is DataGrid && viewModel.SelectedFiles.Any())
            {
                await viewModel.CopySelectedItemsAsync();
            }
        }

        // Метод-обработчик для DragOver
        private void OnDragOver(object? sender, DragEventArgs e)
        {
            // Получаем ViewModel и вызываем её метод
            if (DataContext is HomeViewModel)
            {
                HomeViewModel.OnDragOver(e); // Вызываем метод в ViewModel
            }
        }

        // Метод-обработчик для Drop
        private void OnDrop(object? sender, DragEventArgs e)
        {
            // Получаем ViewModel
            if (DataContext is HomeViewModel vm)
            {
                // Вызываем асинхронный метод из ViewModel, но не ждем его здесь.
                // Avalonia не ждет завершения async обработчика событий.
                _ = vm.OnDropAsync(e); // Используем оператор отбрасывания, чтобы избежать предупреждения CS4014
            }
        }
    }
}