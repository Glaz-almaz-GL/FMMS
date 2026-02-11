using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FMMS.Helpers;
using FMMS.Managers;
using FMMS.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FMMS.ViewModels
{
    public partial class DiskExplorerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _selectedFolderPath = string.Empty;

        [ObservableProperty]
        private bool _isAnalyzing = false;

        [ObservableProperty]
        private string _statusMessage = "Готово";

        [ObservableProperty]
        private long _totalSizeInBytes = 0;

        [ObservableProperty]
        private int _totalFileCount = 0;

        [ObservableProperty]
        private int _totalFolderCount = 0;

        [ObservableProperty]
        private string _folderMessage = "Нет выбранной папки";

        // Для хранения выбранных папок
        [ObservableProperty]
        private ObservableCollection<FolderInfo> _selectedFolders = [];

        public ObservableCollection<FolderInfo> Folders { get; private set; } = [];

        [RelayCommand]
        public void ContextMenuOpenFolder()
        {
            FolderInfo? folder = SelectedFolders.FirstOrDefault();
            if (folder != null)
            {
                OpenFolder(folder);
            }
        }

        [RelayCommand]
        public void ContextMenuOpenContainingFolder()
        {
            FolderInfo? folder = SelectedFolders.FirstOrDefault();
            if (folder != null)
            {
                OpenContainingFolder(folder);
            }
        }

        [RelayCommand]
        public async Task ContextMenuCopyPathsAsync()
        {
            await ClipboardManager.CopyFoldersAsTextAsync(SelectedFolders, SelectedFolderPath);
        }

        [RelayCommand]
        public async Task ContextMenuCopyPathsAsTsvAsync()
        {
            await ClipboardManager.CopyFoldersAsTsvAsync(SelectedFolders, SelectedFolderPath);
        }

        [RelayCommand]
        public async Task SelectAndAnalyzeFolder()
        {
            string? selectedPath = await PathHelper.GetSelectedFolderPathAsync();
            if (selectedPath == null)
            {
                return;
            }

            if (!PathHelper.ValidateFolderPath(selectedPath))
            {
                return;
            }

            SelectedFolderPath = selectedPath;
            await AnalyzeFolderAsync();
        }

        [RelayCommand]
        public async Task AnalyzeFolderAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedFolderPath) || !Directory.Exists(SelectedFolderPath))
            {
                GrowlsManager.ShowWarningMsg("Пожалуйста, выберите существующую папку.");
                return;
            }

            IsAnalyzing = true;
            StatusMessage = "Анализируем...";
            FolderMessage = $"Анализ папки: {SelectedFolderPath}";
            Folders.Clear();

            try
            {
                string[] allSubfolders = Directory.GetDirectories(SelectedFolderPath, "*", SearchOption.AllDirectories);
                List<string> allFoldersToProcess = [SelectedFolderPath, .. allSubfolders];

                foreach (string subfolder in allFoldersToProcess)
                {
                    FolderInfo? info = GetFolderInfo(subfolder, SelectedFolderPath);

                    if (info != null)
                    {
                        Folders.Add(info);
                    }
                }

                List<FolderInfo> sortedFolders = [.. Folders.OrderBy(f => f.RelativePath)];
                Folders.Clear();
                foreach (FolderInfo folder in sortedFolders)
                {
                    Folders.Add(folder);
                }

                TotalSizeInBytes = Folders[0].SizeInBytes;
                TotalFileCount = Folders[0].FileCount;
                TotalFolderCount = Folders.Count;
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex, "Критическая ошибка при анализе корневой папки");
                Folders.Clear();
                TotalSizeInBytes = 0;
                TotalFileCount = 0;
                TotalFolderCount = 0;
            }
            finally
            {
                IsAnalyzing = false;
                StatusMessage = $"Готово. Найдено папок: {Folders.Count}, Файлов: {TotalFileCount}, Общий размер: {FormatSize(TotalSizeInBytes)}";
            }
        }

        [RelayCommand]
        private async Task SaveResults()
        {
            string extension = SettingsManager.CurrentSettings.ExportFileExtension;

            switch (extension)
            {
                case ".txt":
                    await FileExportManager.ExportFoldersToTextAsync(Folders, SelectedFolderPath);
                    break;

                case ".xlsx":
                    await FileExportManager.ExportFoldersToExcelAsync(Folders, SelectedFolderPath);
                    break;
            }
        }

        [RelayCommand]
        public void OpenFolder(FolderInfo? folderInfo = null)
        {
            string? path = folderInfo?.AbsolutePath ?? SelectedFolders.FirstOrDefault()?.AbsolutePath;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    GrowlsManager.ShowErrorMsg(ex, $"Не удалось открыть папку: {path}");
                }
            }
        }

        [RelayCommand]
        public void OpenContainingFolder(FolderInfo? folderInfo = null)
        {
            string? path = folderInfo?.AbsolutePath ?? SelectedFolders.FirstOrDefault()?.AbsolutePath;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.GetDirectoryName(path),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    GrowlsManager.ShowErrorMsg(ex, $"Не удалось открыть содержащую папку для: {path}");
                }
            }
        }

        [RelayCommand]
        public async Task CopyPathsAsync()
        {
            await ClipboardManager.CopyFoldersAsTextAsync(SelectedFolders, SelectedFolderPath);
        }

        private static FolderInfo? GetFolderInfo(string folderPath, string rootPath)
        {
            long size = 0L;
            int count = 0;

            try
            {
                string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    try
                    {
                        FileInfo fileInfo = new(file);
                        size += fileInfo.Length;
                        count++;
                    }
                    catch (UnauthorizedAccessException) { /* Пропускаем файлы, к которым нет доступа */ }
                    catch (FileNotFoundException) { /* Пропускаем файлы, которые были удалены во время анализа */ }
                    catch (IOException) { /* Пропускаем файлы, заблокированные другими процессами */ }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }

            string relativePath = GetRelativePath(folderPath, rootPath);
            return new FolderInfo(relativePath, size, count, folderPath);
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            Uri baseUri = new(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri fullUri = new(fullPath);
            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string FormatSize(long bytes)
        {
            string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public void UpdateSelectedItems(IList selectedItems)
        {
            SelectedFolders.Clear();
            foreach (object? item in selectedItems)
            {
                if (item is FolderInfo myItem)
                {
                    SelectedFolders.Add(myItem);
                }
            }
        }
    }
}