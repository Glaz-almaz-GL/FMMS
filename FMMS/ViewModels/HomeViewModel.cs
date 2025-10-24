using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FMMS.Items;
using FMMS.Managers;
using FMMS.Models;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FMMS.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        public ObservableCollection<FileMetadata> SelectedFiles { get; set; } = [];

        [ObservableProperty]
        private FileMetadata? _selectedFile;

        [ObservableProperty]
        private string _selectedFolderPath = string.Empty;

        [ObservableProperty]
        private int _filesSummaryPageCount = 0;

        [ObservableProperty]
        private bool _shouldEnumerableFiles = true;

        [ObservableProperty]
        private bool _shouldAnalyzeArchives = true;

        [ObservableProperty]
        private bool _isAnalyzing = false;

        [ObservableProperty]
        private double _progressValue = 0;

        [ObservableProperty]
        private bool _isProgressVisible = false;

        [ObservableProperty]
        private string _progressText = string.Empty;

        [ObservableProperty]
        private string _folderMessage = "Нет выбранной папки";

        [ObservableProperty]
        private string _analyzedFolderPath = string.Empty;

        [ObservableProperty]
        private int _totalFilesCount = 0;

        [ObservableProperty]
        private int _totalArchiveFilesCount = 0;

        [ObservableProperty]
        private ColumnSettingsItem _columnSettings = SettingsManager.CurrentSettings.ColumnSettings;

        public ObservableCollection<FileMetadata> FilesAnalyzeResult { get; private set; } = [];

        [RelayCommand]
        public async Task SelectFolderPath()
        {
            IStorageFolder? result = await DialogsManager.OpenSingleFolderDialogAsync("Выберите папку для анализа");
            if (result != null)
            {
                SelectedFolderPath = result.Path.LocalPath;
            }
        }

        [RelayCommand]
        public async Task SaveResults()
        {
            SettingsManager.CurrentSettings.ColumnSettings = ColumnSettings; // Обновляем настройки в SettingsManager
            await SettingsManager.SaveSettingsAsync();

            string extension = SettingsManager.CurrentSettings.ExportFileExtension;

            switch (extension)
            {
                case ".txt":
                    await FileExportManager.ExportToTextAsync(FilesAnalyzeResult, AnalyzedFolderPath);
                    break;

                case ".xlsx":
                    await FileExportManager.ExportToExcelAsync(FilesAnalyzeResult, AnalyzedFolderPath);
                    break;
            }
        }

        [RelayCommand]
        public void OpenFile()
        {
            FileMetadataManager.OpenFile(SelectedFile?.FilePath ?? string.Empty);
        }

        [RelayCommand]
        public void OpenContainingFolder()
        {
            FileMetadataManager.OpenContainingFolder(SelectedFile?.FilePath ?? string.Empty);
        }

        [RelayCommand]
        public async Task CopySelectedItemsAsync()
        {
            await ClipboardManager.CopyAsTextAsync(SelectedFiles, AnalyzedFolderPath);
        }

        [RelayCommand]
        public async Task CopyAsTsvAsync()
        {
            await ClipboardManager.CopyAsTsvAsync(SelectedFiles, AnalyzedFolderPath);
        }

        [RelayCommand]
        public void ShowFileProperties()
        {
            FileMetadataManager.ShowFileProperties(SelectedFile?.FilePath ?? string.Empty);
        }

        [RelayCommand]
        public async Task ProcessFiles(string? selectedFilePath = null)
        {
            SettingsManager.CurrentSettings.ColumnSettings = ColumnSettings; // Обновляем настройки в SettingsManager
            await SettingsManager.SaveSettingsAsync();

            string targetPath = selectedFilePath ?? SelectedFolderPath;

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                GrowlsManager.ShowWarningMsg("Пожалуйста, выберите папку для анализа или перетащите файл/папку.");
                return;
            }

            IsAnalyzing = true;
            IsProgressVisible = true;
            ProgressValue = 0;
            ProgressText = "Начало анализа...";

            try
            {
                await FileProcessingManager.ProcessFilesAsync(
                    targetPath,
                    (msg, val) => { ProgressText = msg; ProgressValue = val; }, // Обновление прогресса
                    msg => ProgressText = msg, // Обновление текста прогресса
                    ShouldEnumerableFiles,
                    ShouldAnalyzeArchives,
                    FilesAnalyzeResult // Передаём ObservableCollection для заполнения
                );

                // Обновляем UI-свойства после завершения
                TotalFilesCount = FilesAnalyzeResult.Count;
                FilesSummaryPageCount = FilesAnalyzeResult.Sum(f => f.PagesCount);
                TotalArchiveFilesCount = FilesAnalyzeResult.Count(f => f.IsArchiveEntry);
                AnalyzedFolderPath = Path.GetDirectoryName(targetPath) ?? string.Empty;

                if (FileProcessingManager.IsArchiveFile(targetPath))
                {
                    FolderMessage = $"Выбран архив: {Path.GetFileName(targetPath)}";
                }
                else if (Directory.Exists(targetPath))
                {
                    FolderMessage = $"Выбранная папка: {targetPath}";
                }
                else
                {
                    FolderMessage = $"Выбран файл: {Path.GetFileName(targetPath)}";
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex, "Произошла ошибка при анализе файлов.");
            }
            finally
            {
                IsAnalyzing = false;
                IsProgressVisible = false;
                ProgressText = string.Empty;
            }
        }

        // Обновлённые методы для Drag & Drop
        public static void OnDragOver(DragEventArgs e)
        {
            e.DragEffects = e.DataTransfer.TryGetFile() != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        public async Task OnDropAsync(DragEventArgs e)
        {
            (bool? isDir, string? droppedFilePath) = DragDropManager.HandleDrop(e);

            if (!string.IsNullOrEmpty(droppedFilePath) && isDir != null)
            {
                if (isDir == true)
                {
                    SelectedFolderPath = droppedFilePath;
                    await ProcessFiles(); // Запускаем анализ сброшенной папки
                }
                else
                {
                    await ProcessFiles(droppedFilePath);
                }
            }
        }

        private void OnFilesAnalyzeResultCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (FilesAnalyzeResult != null)
            {
                if (!ShouldEnumerableFiles)
                {
                    foreach (FileMetadata item in FilesAnalyzeResult)
                    {
                        item.Index = 0;
                    }
                    return;
                }

                for (int i = 0; i < FilesAnalyzeResult.Count; i++)
                {
                    FilesAnalyzeResult[i].Index = i + 1;
                }
            }
        }

        public void UpdateSelectedItems(IList selectedItems)
        {
            SelectedFiles.Clear();
            foreach (object? item in selectedItems)
            {
                if (item is FileMetadata myItem)
                {
                    SelectedFiles.Add(myItem);
                }
            }
        }
    }
}