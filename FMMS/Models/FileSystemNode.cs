using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FMMS.Models
{
    public partial class FileSystemNode : ObservableObject
    {
        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _fullPath;

        [ObservableProperty]
        private bool _isFolder;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFiles))]
        private int _fileCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Size))]
        private double _sizeInBytes;

        public double Size => SizeInBytes / (1024.0 * 1024.0);

        [ObservableProperty]
        private ObservableCollection<FileSystemNode> _children = [];

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasFiles))]
        private bool _isExpanded;

        public bool HasFiles => IsFolder && FileCount > 0 && !IsExpanded;

        public string TypeDisplay => IsFolder ? "Папка" : "Файл";

        public FileSystemNode(string fullPath, bool isFolder, string? displayName = null)
        {
            FullPath = fullPath;
            Name = displayName ?? Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar)) ?? "Корень";
            IsFolder = isFolder;
        }

        public async Task LoadChildrenAsync()
        {
            if (!IsFolder) return;

            IsLoading = true;
            Children.Clear();

            try
            {
                string[] subdirectories = Directory.GetDirectories(FullPath);
                string[] files = Directory.GetFiles(FullPath);

                foreach (string dirPath in subdirectories)
                {
                    var childNode = new FileSystemNode(dirPath, true);
                    Children.Add(childNode);
                }

                foreach (string filePath in files)
                {
                    var childNode = new FileSystemNode(filePath, false);
                    Children.Add(childNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Игнорируем недоступные папки
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex, "Ошибка обработки папки");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Рекурсивно загружает всю структуру папок и файлов начиная с текущего узла до указанной глубины.
        /// </summary>
        /// <param name="maxDepth">Максимальная глубина рекурсии. -1 означает без ограничений.</param>
        /// <param name="currentDepth">Текущая глубина рекурсии (используется внутренне).</param>
        /// <param name="logCallback">Делегат для обновления лога в UI (передает путь к текущей папке).</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Кортеж (FileCount, SizeInBytes)</returns>
        public async Task<(int fileCount, double sizeInBytes)> LoadFullTreeAsync(
            int maxDepth = -1,
            int currentDepth = 0,
            Action<string>? logCallback = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (!IsFolder)
            {
                try
                {
                    var info = new FileInfo(FullPath);
                    SizeInBytes = info.Length;
                    FileCount = 1;
                    return (1, info.Length);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or FileNotFoundException)
                {
                    // Игнорируем файлы, к которым нет доступа
                    SizeInBytes = 0;
                    FileCount = 0;
                    return (0, 0);
                }
                catch
                {
                    // На всякий случай, другие исключения тоже возвращают 0
                    SizeInBytes = 0;
                    FileCount = 0;
                    return (0, 0);
                }
            }

            logCallback?.Invoke(FullPath);

            // Загружаем содержимое папки
            Children.Clear();

            string[] subdirectories;
            string[] files;

            try
            {
                subdirectories = Directory.GetDirectories(FullPath);
                files = Directory.GetFiles(FullPath);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException)
            {
                SizeInBytes = 0;
                FileCount = 0;
                return (0, 0);
            }

            int totalFiles = 0;
            double totalSize = 0;

            // Подсчитываем файлы
            foreach (string filePath in files)
            {
                ct.ThrowIfCancellationRequested();
                var childNode = new FileSystemNode(filePath, false);
                await Dispatcher.UIThread.InvokeAsync(() => Children.Add(childNode));

                try
                {
                    var fileInfo = new FileInfo(childNode.FullPath);
                    childNode.SizeInBytes = fileInfo.Length;
                    childNode.FileCount = 1;
                    totalFiles++;
                    totalSize += fileInfo.Length;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or FileNotFoundException)
                {
                    // Игнорируем файлы, к которым нет доступа
                    childNode.SizeInBytes = 0;
                    childNode.FileCount = 0;
                }
            }

            // Добавляем папки в Children
            foreach (string dirPath in subdirectories)
            {
                ct.ThrowIfCancellationRequested();
                var childNode = new FileSystemNode(dirPath, true);
                await Dispatcher.UIThread.InvokeAsync(() => Children.Add(childNode));
            }

            var folderChildren = Children.Where(c => c.IsFolder).ToList();

            // Рекурсивно обрабатываем папки
            if (folderChildren.Count > 0)
            {
                var tasks = new List<Task<(int fileCount, double sizeInBytes)>>();

                foreach (var child in folderChildren)
                {
                    if (maxDepth >= 0 && currentDepth + 1 >= maxDepth)
                    {
                        continue;
                    }

                    // Оборачиваем вызов в анонимную задачу с try-catch
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            return await child.LoadFullTreeAsync(maxDepth, currentDepth + 1, logCallback, ct);
                        }
                        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
                        {
                            // Игнорируем ошибки доступа к подпапке
                            return (0, 0);
                        }
                    }, ct));
                }

                var results = await Task.WhenAll(tasks);

                foreach (var (childFiles, childSize) in results)
                {
                    totalFiles += childFiles;
                    totalSize += childSize;
                }
            }

            FileCount = totalFiles;
            SizeInBytes = totalSize;

            return (totalFiles, totalSize);
        }

        [RelayCommand]
        private static async Task ExpandAllChildrenAsync(FileSystemNode node)
        {
            if (node != null)
            {
                if (!Dispatcher.UIThread.CheckAccess())
                {
                    await Dispatcher.UIThread.InvokeAsync(() => ExpandAllChildrenAsync(node));
                    return;
                }

                if (node?.IsFolder == true)
                {
                    node.IsExpanded = true;

                    await Task.Delay(300);

                    // Рекурсивно вызываем для детей
                    foreach (var child in node.Children)
                    {
                        if (child.IsFolder)
                        {
                            await ExpandAllChildrenAsync(child);
                        }
                    }
                }
            }
        }
    }
}