using FMMS.Items;
using FMMS.Models;
using iText.Kernel.Pdf;
using SharpCompress.Archives;
using SharpCompress.Common;
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
            ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".tb2", ".xz", ".txz", ".lz", ".tlz", ".z", ".lzma", ".lzo", ".ar", ".cpio", ".iso", ".dmg", ".wim", ".esd", ".squashfs", ".cramfs", ".jar", ".war", ".apk", ".xpi", ".epub", ".s7z"
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

            CreateFileMetadataParameters parameters = new(
                    FilePathOrEntryKey: archivePath,
                    AnalyzedRootPath: Path.GetDirectoryName(archivePath) ?? string.Empty,
                    IsArchive: true,
                    IsEntry: false,
                    ArchivePath: string.Empty
            );

            // 1. Добавляем сам архив как FileMetadata
            FileMetadata archiveMetadata = await CreateFileMetadataAsync(parameters);
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
                await using FileStream archiveStream = File.OpenRead(archivePath);
                using IArchive archive = ArchiveFactory.Open(archiveStream);

                async Task<Stream?> extractStreamFunc(string entryKey)
                {
                    IArchiveEntry? entry = archive.Entries.FirstOrDefault(e => e.Key == entryKey && !e.IsDirectory);
                    if (entry != null)
                    {
                        Stream entryStream = await entry.OpenEntryStreamAsync();
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

                    CreateFileMetadataParameters parameters = new(
                    FilePathOrEntryKey: entry.Key,
                    AnalyzedRootPath: Path.GetDirectoryName(archivePath) ?? string.Empty,
                    IsArchive: false,
                    IsEntry: true,
                    ArchivePath: archivePath,
                    entry.CompressedSize,
                    entry.Size,
                    extractStreamFunc
            );

                    FileMetadata entryMetadata = await CreateFileMetadataAsync(parameters);

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
                await ProcessArchiveFileAsync(filePath, (_, _) => { }, _ => { }, shouldEnumerableFiles, resultCollection); // Простой прогресс для одиночного архива
                return;
            }

            CreateFileMetadataParameters parameters = new(
                    FilePathOrEntryKey: filePath,
                    AnalyzedRootPath: Path.GetDirectoryName(filePath) ?? string.Empty,
                    IsArchive: false,
                    IsEntry: false,
                    ArchivePath: string.Empty
            );

            FileMetadata fileMetadata = await CreateFileMetadataAsync(parameters);
            resultCollection.Add(fileMetadata);

            ApplyIndexing(resultCollection, shouldEnumerableFiles);
        }

        private static async Task ProcessDirectoryAsync(string directoryPath, Action<string, double> updateProgress, Action<string> updateProgressText, bool shouldEnumerableFiles, bool shouldAnalyzeArchives, ObservableCollection<FileMetadata> resultCollection)
        {
            resultCollection.Clear();

            List<string> allFiles = [.. Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)];
            List<string> archiveFiles = [.. allFiles.Where(IsArchiveFile)];
            List<string> regularFiles = [.. allFiles.Except(archiveFiles)];

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

                CreateFileMetadataParameters parameters = new(
                    FilePathOrEntryKey: filePath,

                    AnalyzedRootPath: directoryPath,
                    IsArchive: false,
                    IsEntry: false,
                    ArchivePath: string.Empty
                );

                FileMetadata fileMetadata = await CreateFileMetadataAsync(parameters);
                resultCollection.Add(fileMetadata);
            }

            foreach (string archivePath in archiveFiles)
            {
                currentFileIndex++;
                string archiveFileName = Path.GetFileName(archivePath);

                updateProgressText($"Обработка архива {currentFileIndex} из {totalItems}: {archiveFileName}");
                updateProgress($"Обработка архива {currentFileIndex} из {totalItems}: {archivePath}", (double)currentFileIndex / totalItems * 100);

                CreateFileMetadataParameters parameters = new(
                    FilePathOrEntryKey: archivePath,
                    AnalyzedRootPath: directoryPath,
                    IsArchive: true,
                    IsEntry: false,
                    ArchivePath: string.Empty
                );

                FileMetadata archiveMetadata = await CreateFileMetadataAsync(parameters);
                resultCollection.Add(archiveMetadata);

                if (shouldAnalyzeArchives)
                {
                    await ProcessArchiveEntriesAsync(archivePath, resultCollection, updateProgress, updateProgressText);
                }
            }

            ApplyIndexing(resultCollection, shouldEnumerableFiles);
        }

        /// <summary>
        /// Асинхронно создает объект FileMetadata, анализируя файл или запись в архиве.
        /// </summary>
        private static async Task<FileMetadata> CreateFileMetadataAsync(CreateFileMetadataParameters parameters)
        {
            var (
                filePathOrEntryKey,
                analyzedRootPath,
                isArchive,
                isEntry,
                archivePath,
                compressedSizeBytes,
                uncompressedSizeBytes,
                extractStreamFunc
            ) = parameters;

            // Общие свойства
            string fileExtension = Path.GetExtension(filePathOrEntryKey);
            string fileName = Path.GetFileName(filePathOrEntryKey);

            long fileSizeBytes = isEntry ? uncompressedSizeBytes ?? 0 : new FileInfo(filePathOrEntryKey).Length;

            // Определение свойств, зависящих от типа (архив/запись/обычный файл)
            var (sha256, pagesCount, fileRelativePath, folderRelativePath) = isEntry
                ? await ProcessArchiveEntryAsync(filePathOrEntryKey, analyzedRootPath, archivePath, fileExtension, extractStreamFunc)
                : await ProcessRegularFileAsync(filePathOrEntryKey, analyzedRootPath, fileExtension);

            return new FileMetadata
            {
                FilePath = filePathOrEntryKey,
                FileRelativePath = fileRelativePath,
                FolderRelativePath = string.IsNullOrWhiteSpace(folderRelativePath) || folderRelativePath.Trim() == "\\" ? string.Empty : folderRelativePath,
                FileName = fileName,
                FileExtension = fileExtension,
                FileSHA256 = sha256,
                PagesCount = pagesCount,
                FileSizeBytes = fileSizeBytes,
                IsArchiveFile = isArchive,
                IsArchiveEntry = isEntry,
                ArchiveFilePath = archivePath,
                CompressedSizeBytes = compressedSizeBytes,
                UncompressedSizeBytes = uncompressedSizeBytes
            };
        }

        /// <summary>
        /// Обрабатывает запись внутри архива.
        /// </summary>
        private static async Task<(string sha256, int pagesCount, string fileRelativePath, string? folderRelativePath)> ProcessArchiveEntryAsync(
            string entryKey,
            string analyzedRootPath,
            string archivePath,
            string fileExtension,
            Func<string, Task<Stream?>>? extractStreamFunc)
        {
            // Вычисление относительных путей для записи в архиве
            string archiveRelativePathFromRoot = '\\' + Path.GetRelativePath(analyzedRootPath, archivePath).Replace('/', Path.DirectorySeparatorChar);
            string archivePathWithoutExtension = Path.ChangeExtension(archiveRelativePathFromRoot, null);
            string entryRelativePath = entryKey.Replace('/', Path.DirectorySeparatorChar);
            string fileRelativePath = archivePathWithoutExtension + Path.DirectorySeparatorChar + entryRelativePath;

            string? entryDir = Path.GetDirectoryName(entryRelativePath);
            string? folderRelativePath = string.IsNullOrEmpty(entryDir) ? archivePathWithoutExtension : archivePathWithoutExtension + Path.DirectorySeparatorChar + entryDir;

            // Инициализация результатов
            string sha256 = string.Empty;
            int pagesCount;

            if (extractStreamFunc == null)
            {
                GrowlsManager.ShowErrorMsg($"Невозможно обработать: функция извлечения потока отсутствует для: {entryKey} из архива {archivePath}");
                return (sha256, -1, fileRelativePath, folderRelativePath); // Возвращаем ошибку
            }

            await using Stream? entryStream = await extractStreamFunc(entryKey);
            if (entryStream == null)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось извлечь поток: {entryKey} из архива {archivePath}");
                return (string.Empty, -1, fileRelativePath, folderRelativePath); // Возвращаем ошибку
            }

            // Вычисление SHA256
            sha256 = await CalculateSHA256ForStreamAsync(entryStream);

            // Определение количества страниц
            pagesCount = await DeterminePageCountForStreamAsync(entryStream, fileExtension, entryKey, archivePath);

            return (sha256, pagesCount, fileRelativePath, folderRelativePath);
        }

        /// <summary>
        /// Обрабатывает обычный файл на диске.
        /// </summary>
        private static async Task<(string sha256, int pagesCount, string fileRelativePath, string? folderRelativePath)> ProcessRegularFileAsync(
            string filePath,
            string analyzedRootPath,
            string fileExtension)
        {
            // Вычисление относительных путей
            string fileRelativePath = '\\' + Path.GetRelativePath(analyzedRootPath, filePath);
            string? folderRelativePath = Path.GetDirectoryName(fileRelativePath);

            // Вычисление SHA256
            string sha256 = await FilesHashManager.GetSha256HashAsync(filePath);

            // Определение количества страниц
            int pagesCount = await DeterminePageCountForFileAsync(filePath, fileExtension);

            return (sha256, pagesCount, fileRelativePath, folderRelativePath);
        }

        /// <summary>
        /// Вычисляет SHA256 для потока, сбросив позицию в начало после.
        /// </summary>
        private static async Task<string> CalculateSHA256ForStreamAsync(Stream stream)
        {
            try
            {
                if (stream.CanSeek)
                {
                    string hash = await FilesHashManager.GetSha256HashAsync(stream);
                    stream.Position = 0; // Сброс позиции для последующего чтения
                    return hash;
                }
                else
                {
                    GrowlsManager.ShowErrorMsg("Поток не поддерживает Seek, невозможно вычислить SHA256.");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg(ex, "Ошибка вычисления SHA256 для потока.");
                return string.Empty;
            }
        }

        /// <summary>
        /// Определяет количество страниц для потока (PDF или изображение).
        /// </summary>
        private static async Task<int> DeterminePageCountForStreamAsync(Stream stream, string fileExtension, string entryKey, string archivePath)
        {
            int pagesCount = 0;

            if (fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using PdfDocument pdfDoc = new(new PdfReader(stream));
                    pagesCount = pdfDoc.GetNumberOfPages();
                }
                catch (Exception ex)
                {
                    GrowlsManager.ShowErrorMsg(ex, $"Ошибка чтения PDF: {entryKey} из архива {archivePath}");
                    pagesCount = -1;
                }
            }
            else if (IsImage(entryKey))
            {
                pagesCount = 1; // Изображение считается за 1 страницу
            }

            return pagesCount;
        }

        /// <summary>
        /// Определяет количество страниц для файла на диске (PDF или изображение).
        /// </summary>
        private static async Task<int> DeterminePageCountForFileAsync(string filePath, string fileExtension)
        {
            int pagesCount = 0;

            if (fileExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using PdfDocument pdfDoc = new(new PdfReader(filePath));
                    pagesCount = pdfDoc.GetNumberOfPages();
                }
                catch (Exception ex)
                {
                    GrowlsManager.ShowErrorMsg(ex, $"Ошибка чтения PDF: {filePath}");
                    pagesCount = -1;
                }
            }
            else if (IsImage(filePath))
            {
                pagesCount = 1; // Изображение считается за 1 страницу
            }

            return pagesCount;
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