using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FMMS.Managers;
using FMMS.Models;
using Shell32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FMMS.ViewModels
{
    public partial class DiskExplorerViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<FileSystemNode> _rootNodes = [];

        [ObservableProperty]
        private int _totalFolders;

        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private double _totalSize; // В мегабайтах

        [ObservableProperty]
        private bool _isReady;

        [ObservableProperty]
        private bool _isInitializing;

        [ObservableProperty]
        private bool _isLoadingStructure;

        [ObservableProperty]
        private FileSystemNode? _selectedNode;

        [ObservableProperty]
        private string _currentLogMessage = string.Empty;

        [RelayCommand]
        private async Task RefreshAsync()
        {
            IsLoadingStructure = true;
            RootNodes.Clear();
            TotalFolders = 0;
            TotalFiles = 0;
            TotalSize = 0.0;

            // Создаем делегат для обновления лога из FileSystemNode
            void updateLogAction(string path)
            {
                CurrentLogMessage = $"Обработка папки: {path}";
            }

            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                var tasks = new List<Task>();

                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady && (drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable))
                    {
                        // Формируем имя диска в нужном формате
                        tasks.Add(Task.Run(async () =>
                        {
                            string displayName;
                            if (drive.DriveType == DriveType.Fixed)
                            {
                                displayName = string.IsNullOrEmpty(drive.VolumeLabel)
                                    ? $"Локальный диск {drive.Name[0]}"
                                    : $"Локальный диск {drive.Name[0]} ({drive.VolumeLabel})";
                            }
                            else // Removable
                            {
                                displayName = string.IsNullOrEmpty(drive.VolumeLabel)
                                    ? $"Съёмный диск {drive.Name[0]}"
                                    : $"Съёмный диск {drive.Name[0]} ({drive.VolumeLabel})";
                            }

                            CurrentLogMessage = $"Начало сканирования диска {displayName}...";

                            var rootNode = new FileSystemNode(drive.RootDirectory.FullName, true, displayName);

                            lock (RootNodes)
                            {
                                RootNodes.Add(rootNode);
                            }

                            var (fileCount, sizeInBytes) = await rootNode.LoadFullTreeAsync(maxDepth: -1, logCallback: updateLogAction);

                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                rootNode.FileCount = fileCount;
                                rootNode.SizeInBytes = sizeInBytes;
                            });

                            // Теперь используем возвращённые значения для обновления итогов
                            var totalsToAdd = (fileCount, sizeInBytes, folders: await CountFoldersInMemory(rootNode));

                            // Обновляем UI-свойства через Dispatcher
                            _ = Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                TotalFiles += totalsToAdd.fileCount;
                                TotalSize += totalsToAdd.sizeInBytes / (1024.0 * 1024.0); // MB
                                TotalFolders += totalsToAdd.folders;
                            });
                        }));
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                CurrentLogMessage = "Сканирование отменено.";
            }
            catch (Exception ex)
            {
                var errorMsg = $"Ошибка при сканировании дисков: {ex.Message}";
                GrowlsManager.ShowErrorMsg(ex, errorMsg);
                CurrentLogMessage = errorMsg;
            }
            finally
            {
                IsReady = true;
                IsLoadingStructure = false;
            }
        }

        private static async Task<int> CountFoldersInMemory(FileSystemNode node)
        {
            int count = node.IsFolder ? 1 : 0;
            foreach (var child in node.Children)
            {
                count += await CountFoldersInMemory(child);
            }
            return count;
        }
    }
}