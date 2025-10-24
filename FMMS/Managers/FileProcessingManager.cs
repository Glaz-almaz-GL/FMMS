using FMMS.Items;
using FMMS.Models;
using iText.Kernel.Pdf;
using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class FileProcessingManager
    {
        private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".tbz2", ".xz", ".txz", ".lz", ".tlz", ".z", ".lzma", ".lzo", ".ar", ".cpio", ".iso", ".dmg", ".wim", ".esd", ".squashfs", ".cramfs", ".jar", ".war", ".apk", ".xpi", ".epub", ".s7z"
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpeg", ".jpg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".psd", ".raw", ".svg", ".svgz", ".webp",
            ".heif", ".heic", ".avif", ".cr2", ".nef", ".arw", ".dng", ".crw", ".tga", ".ico", ".pcx", ".pbm",
            ".pgm", ".ppm", ".dds", ".exr", ".hdr", ".jxr", ".pxr"
        };

        public static async Task ProcessFilesAsync(string targetPath, Action<string, double> updateProgress, Action<string> updateProgressText, bool shouldEnumerableFiles, bool shouldAnalyzeArchives, ObservableCollection<FileMetadata> resultCollection)
        {
            bool isFile = File.Exists(targetPath);
            bool isDirectory = Directory.Exists(targetPath);
            bool isArchive = IsArchiveFile(targetPath);

            if (!isFile && !isDirectory)
            {
                GrowlsManager.ShowErrorMsg("Указанный путь не существует как файл или папка.");
                return;
            }

            if (isFile)
            {
                if (isArchive && shouldAnalyzeArchives)
                {
                    await ProcessArchiveFileAsync(targetPath, updateProgress, updateProgressText, shouldEnumerableFiles, resultCollection);
                }
                else
                {
                    await ProcessSingleFileAsync(targetPath, shouldEnumerableFiles, resultCollection);
                }
                return;
            }

            if (isDirectory)
            {
                await ProcessDirectoryAsync(targetPath, updateProgress, updateProgressText, shouldEnumerableFiles, shouldAnalyzeArchives, resultCollection);
            }
        }

        private static async Task ProcessArchiveFileAsync(string archivePath, Action<string, double> updateProgress, Action<string> updateProgressText, bool shouldEnumerableFiles, ObservableCollection<FileMetadata> resultCollection)
        {
            updateProgressText($"Обработка архива: {Path.GetFileName(archivePath)}");

            resultCollection.Clear();

            // 1. Добавляем сам архив как FileMetadata
            FileMetadata archiveMetadata = await CreateFileMetadataAsync(archivePath, Path.GetDirectoryName(archivePath) ?? string.Empty, true, false, string.Empty);
            resultCollection.Add(archiveMetadata);
            updateProgress($"Обработка архива: {Path.GetFileName(archivePath)}", 50); // Примерный прогресс

            // 2. Извлекаем и добавляем содержимое архива
            await ProcessArchiveEntriesAsync(archivePath, resultCollection, updateProgress, updateProgressText);

            ApplyIndexing(resultCollection, shouldEnumerableFiles);
        }

        private static async Task ProcessArchiveEntriesAsync(string archivePath, ObservableCollection<FileMetadata> resultCollection, Action<string, double> updateProgress, Action<string> updateProgressText)
        {
            try
            {
                using FileStream archiveStream = File.OpenRead(archivePath);
                using IArchive archive = ArchiveFactory.Open(archiveStream);

                async Task<Stream?> extractStreamFunc(string entryKey)
                {
                    IArchiveEntry? entry = archive.Entries.FirstOrDefault(e => e.Key == entryKey && !e.IsDirectory);
                    if (entry != null)
                    {
                        Stream entryStream = entry.OpenEntryStream();
                        if (entryStream != null)
                        {
                            MemoryStream memoryStream = new();
                            await entryStream.CopyToAsync(memoryStream);
                            memoryStream.Position = 0;
                            await entryStream.DisposeAsync();
                            return memoryStream;
                        }
                    }
                    return null;
                }

                int entryCount = archive.Entries.Count(e => !e.IsDirectory);
                int processedEntries = 0;

                foreach (IArchiveEntry? entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    if (string.IsNullOrEmpty(entry?.Key))
                    {
                        continue;
                    }

                    if (entry.Key.StartsWith("__MACOSX/", StringComparison.OrdinalIgnoreCase) ||
                        entry.Key.EndsWith("/.DS_Store", StringComparison.OrdinalIgnoreCase) ||
                        entry.Key.Contains(".DS_Store"))
                    {
                        continue;
                    }

                    FileMetadata entryMetadata = await CreateFileMetadataAsync(
                        entry.Key,
                        Path.GetDirectoryName(archivePath) ?? string.Empty,
                        false, // isArchive
                        true,  // isEntry
                        archivePath,
                        entry.Size,
                        entry.Size,
                        extractStreamFunc
                    );

                    resultCollection.Add(entryMetadata);

                    processedEntries++;
                    updateProgressText($"Обработка записи архива {processedEntries} из {entryCount}: {Path.GetFileName(entry.Key)}");
                    updateProgress($"Обработка записи архива {processedEntries} из {entryCount}: {Path.GetFileName(entry.Key)}", (double)processedEntries / entryCount * 100);
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex, $"Ошибка чтения архива: {archivePath}");
            }
        }

        private static async Task ProcessSingleFileAsync(string filePath, bool shouldEnumerableFiles, ObservableCollection<FileMetadata> resultCollection)
        {
            resultCollection.Clear();

            if (IsArchiveFile(filePath))
            {
                await ProcessArchiveFileAsync(filePath, (msg, val) => { }, msg => { }, shouldEnumerableFiles, resultCollection); // Простой прогресс для одиночного архива
                return;
            }

            FileMetadata fileMetadata = await CreateFileMetadataAsync(filePath, Path.GetDirectoryName(filePath) ?? string.Empty, false, false, string.Empty);
            resultCollection.Add(fileMetadata);

            ApplyIndexing(resultCollection, shouldEnumerableFiles);
        }

        private static async Task ProcessDirectoryAsync(string directoryPath, Action<string, double> updateProgress, Action<string> updateProgressText, bool shouldEnumerableFiles, bool shouldAnalyzeArchives, ObservableCollection<FileMetadata> resultCollection)
        {
            resultCollection.Clear();

            List<string> allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();
            List<string> archiveFiles = allFiles.Where(IsArchiveFile).ToList();
            List<string> regularFiles = allFiles.Except(archiveFiles).ToList();

            int totalItems = allFiles.Count;

            if (shouldAnalyzeArchives)
            {
                totalItems += archiveFiles.Sum(GetArchiveEntryCount);
            }

            int currentFileIndex = 0;

            foreach (string filePath in regularFiles)
            {
                currentFileIndex++;
                string fileName = Path.GetFileName(filePath);
                updateProgressText($"Обработка файла {currentFileIndex} из {totalItems}: {fileName}");
                updateProgress($"Обработка файла {currentFileIndex} из {totalItems}: {fileName}", (double)currentFileIndex / totalItems * 100);

                FileMetadata fileMetadata = await CreateFileMetadataAsync(filePath, directoryPath, false, false, string.Empty);
                resultCollection.Add(fileMetadata);
            }

            foreach (string archivePath in archiveFiles)
            {
                currentFileIndex++;
                string archiveFileName = Path.GetFileName(archivePath);

                updateProgressText($"Обработка архива {currentFileIndex} из {totalItems}: {archiveFileName}");
                updateProgress($"Обработка архива {currentFileIndex} из {totalItems}: {archivePath}", (double)currentFileIndex / totalItems * 100);

                FileMetadata archiveMetadata = await CreateFileMetadataAsync(archivePath, directoryPath, true, false, string.Empty);
                resultCollection.Add(archiveMetadata);

                if (shouldAnalyzeArchives)
                {
                    await ProcessArchiveEntriesAsync(archivePath, resultCollection, updateProgress, updateProgressText);
                }
            }

            ApplyIndexing(resultCollection, shouldEnumerableFiles);
        }

        private static async Task<FileMetadata> CreateFileMetadataAsync(
            string filePathOrEntryKey,
            string analyzedRootPath,
            bool isArchive,
            bool isEntry,
            string archivePath = "",
            long? compressedSize = null,
            long? uncompressedSize = null,
            Func<string, Task<Stream?>>? extractStreamFunc = null
        )
        {
            int pagesCount = 0;
            string fileExtension = Path.GetExtension(filePathOrEntryKey);
            string sha256 = string.Empty;
            string fileName = Path.GetFileName(filePathOrEntryKey);
            string fileRelativePath;
            string? folderRelativePath;
            long fileSizeBytes = uncompressedSize ?? 0;

            if (isEntry)
            {
                string archiveRelativePathFromRoot = '\\' + Path.GetRelativePath(analyzedRootPath, archivePath).Replace('/', Path.DirectorySeparatorChar);
                string archivePathWithoutExtension = Path.ChangeExtension(archiveRelativePathFromRoot, null);
                string entryRelativePath = filePathOrEntryKey.Replace('/', Path.DirectorySeparatorChar);
                fileRelativePath = archivePathWithoutExtension + Path.DirectorySeparatorChar + entryRelativePath;

                string? entryDir = Path.GetDirectoryName(entryRelativePath);
                folderRelativePath = string.IsNullOrEmpty(entryDir) ? archivePathWithoutExtension : archivePathWithoutExtension + Path.DirectorySeparatorChar + entryDir;

                if (extractStreamFunc != null)
                {
                    using Stream? entryStream = await extractStreamFunc(filePathOrEntryKey);
                    if (entryStream != null)
                    {
                        try
                        {
                            if (entryStream.CanSeek)
                            {
                                sha256 = await FilesHashManager.GetSha256HashAsync(entryStream);
                                entryStream.Position = 0;
                            }
                            else
                            {
                                GrowlsManager.ShowErrorMsg($"Поток для {filePathOrEntryKey} не поддерживает Seek.");
                                sha256 = string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            sha256 = string.Empty;
                            GrowlsManager.ShowErrorMsg(ex, $"Ошибка вычисления SHA256 для: {filePathOrEntryKey} из архива {archivePath}");
                        }

                        if (fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                using PdfDocument pdfDoc = new(new PdfReader(entryStream));
                                pagesCount = pdfDoc.GetNumberOfPages();
                            }
                            catch (Exception ex)
                            {
                                pagesCount = -1;
                                GrowlsManager.ShowErrorMsg(ex, $"Ошибка чтения PDF: {filePathOrEntryKey} из архива {archivePath}");
                            }
                        }

                        if (pagesCount == 0 && IsImage(filePathOrEntryKey))
                        {
                            pagesCount = 1;
                        }
                    }
                    else
                    {
                        sha256 = string.Empty;
                        pagesCount = -1;
                        GrowlsManager.ShowErrorMsg($"Не удалось извлечь поток: {filePathOrEntryKey} из архива {archivePath}");
                    }
                }
                else
                {
                    sha256 = string.Empty;
                    pagesCount = -1;
                    GrowlsManager.ShowErrorMsg($"Невозможно обработать: функция извлечения потока отсутствует для: {filePathOrEntryKey} из архива {archivePath}");
                }
            }
            else
            {
                // Для обычных файлов получаем размер через FileInfo
                try
                {
                    FileInfo fileInfo = new(filePathOrEntryKey);
                    fileSizeBytes = fileInfo.Length;
                }
                catch (Exception ex)
                {
                    GrowlsManager.ShowErrorMsg(ex, $"Ошибка получения размера файла: {filePathOrEntryKey}");
                    fileSizeBytes = 0; // Или -1, если нужно отличать ошибки
                }

                sha256 = await FilesHashManager.GetSha256HashAsync(filePathOrEntryKey);
                fileRelativePath = '\\' + Path.GetRelativePath(analyzedRootPath, filePathOrEntryKey);
                folderRelativePath = Path.GetDirectoryName(fileRelativePath);

                if (fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using PdfDocument pdfDoc = new(new PdfReader(filePathOrEntryKey));
                        pagesCount = pdfDoc.GetNumberOfPages();
                    }
                    catch (Exception ex)
                    {
                        GrowlsManager.ShowErrorMsg(ex);
                        pagesCount = -1;
                    }
                }
                else if (IsImage(filePathOrEntryKey))
                {
                    pagesCount = 1;
                }
            }

            return new FileMetadata
            {
                FilePath = filePathOrEntryKey,
                FileRelativePath = fileRelativePath,
                FolderRelativePath = string.IsNullOrWhiteSpace(folderRelativePath) || folderRelativePath.Trim() == "\\" ? string.Empty : folderRelativePath,
                FileName = fileName,
                FileExtension = fileExtension,
                FileSHA256 = sha256,
                PagesCount = pagesCount,
                FileSizeBytes = fileSizeBytes, // Добавляем размер
                IsArchiveFile = isArchive,
                IsArchiveEntry = isEntry,
                ArchiveFilePath = archivePath,
                CompressedSize = compressedSize,
                UncompressedSize = uncompressedSize
            };
        }

        private static void ApplyIndexing(ObservableCollection<FileMetadata> collection, bool shouldEnumerableFiles)
        {
            if (shouldEnumerableFiles)
            {
                for (int i = 0; i < collection.Count; i++)
                {
                    collection[i].Index = i + 1;
                }
            }
            else
            {
                foreach (FileMetadata item in collection)
                {
                    item.Index = null;
                }
            }
        }

        public static bool IsArchiveFile(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            return ArchiveExtensions.Contains(ext);
        }

        public static bool IsImage(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            return ImageExtensions.Contains(ext);
        }

        private static int GetArchiveEntryCount(string archivePath)
        {
            try
            {
                using FileStream archiveStream = File.OpenRead(archivePath);
                using IArchive archive = ArchiveFactory.Open(archiveStream);
                return archive.Entries.Count(entry => !entry.IsDirectory);
            }
            catch
            {
                return 0;
            }
        }
    }
}