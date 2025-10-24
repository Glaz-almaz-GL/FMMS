using Avalonia.Controls;
using Avalonia.Input.Platform;
using FMMS.Items;
using FMMS.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class ClipboardManager
    {
        private static TopLevel? _topLevel = null;

        public static void Initialize(TopLevel? topLevel)
        {
            _topLevel = topLevel;
        }

        /// <summary>
        /// Копирует пути выбранных файлов в буфер обмена.
        /// </summary>
        /// <param name="selectedFiles">Коллекция выбранных файлов.</param>
        /// <param name="topLevel">Экземпляр TopLevel для доступа к буферу обмена.</param>
        public static async Task CopyAsTextAsync(IList<FileMetadata> itemsToProcess, string selectedFolderPath)
        {
            try
            {
                IClipboard? clipboard = _topLevel?.Clipboard;
                if (clipboard != null)
                {
                    string compiledText = await CompileTextAsync(itemsToProcess, selectedFolderPath);

                    await clipboard.SetTextAsync(compiledText);
                }
                else
                {
                    GrowlsManager.ShowErrorMsg("Не удалось получить доступ к буферу обмена.");
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка копирования: {ex.Message}");
            }
        }

        public static async Task<string> CompileTextAsync(
     IList<FileMetadata> itemsToProcess,
     string selectedFolderPath,
     ColumnSettingsItem? columnSettings = null)
        {
            columnSettings ??= SettingsManager.CurrentSettings.ColumnSettings;

            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowWarningMsg("Нет выделенных элементов для копирования.");
                return string.Empty;
            }

            StringBuilder sb = new();
            sb.AppendLine($"Путь до проанализированной папки: {selectedFolderPath}");
            sb.AppendLine();

            // --- Новый блок: Определение видимых столбцов и их максимальных ширин ---
            int maxIndexWidth = 0;
            int maxFileNameWidth = 0;
            int maxFolderRelativePathWidth = 0;
            int maxPagesCountWidth = 0;
            int maxFileExtensionWidth = 0;
            int maxFileSHA256Width = 0;
            int maxFilePathWidth = 0;
            int maxFileRelativePathWidth = 0;
            int maxArchiveFileWidth = 0;
            int maxArchiveEntryWidth = 0;
            int maxArchiveFilePathWidth = 0;
            int maxCompressedSizeWidth = 0;
            int maxUncompressedSizeWidth = 0;
            int maxFileSizeMBWidth = 0;
            int maxFileSizeBytesWidth = 0;

            // Проходим по всем элементам, чтобы найти максимальную длину для каждого видимого столбца
            foreach (FileMetadata item in itemsToProcess)
            {
                if (columnSettings.IsIndexColumnVisible && item.Index.HasValue)
                {
                    maxIndexWidth = Math.Max(maxIndexWidth, item.Index.Value.ToString().Length);
                }
                if (columnSettings.IsFileNameColumnVisible)
                {
                    maxFileNameWidth = Math.Max(maxFileNameWidth, item.FileName.Length);
                }
                if (columnSettings.IsFolderRelativePathColumnVisible)
                {
                    maxFolderRelativePathWidth = Math.Max(maxFolderRelativePathWidth, item.FolderRelativePath.Length);
                }
                if (columnSettings.IsPagesCountColumnVisible)
                {
                    maxPagesCountWidth = Math.Max(maxPagesCountWidth, item.PagesCount.ToString().Length);
                }
                if (columnSettings.IsFileExtensionColumnVisible)
                {
                    maxFileExtensionWidth = Math.Max(maxFileExtensionWidth, item.FileExtension.Length);
                }
                if (columnSettings.IsFileSHA256ColumnVisible)
                {
                    maxFileSHA256Width = Math.Max(maxFileSHA256Width, item.FileSHA256.Length);
                }
                if (columnSettings.IsFilePathColumnVisible)
                {
                    maxFilePathWidth = Math.Max(maxFilePathWidth, item.FilePath.Length);
                }
                if (columnSettings.IsFileRelativePathColumnVisible)
                {
                    maxFileRelativePathWidth = Math.Max(maxFileRelativePathWidth, item.FileRelativePath.Length);
                }
                if (columnSettings.IsArchiveFileColumnVisible)
                {
                    maxArchiveFileWidth = Math.Max(maxArchiveFileWidth, item.IsArchiveFile.ToString().Length);
                }
                if (columnSettings.IsArchiveEntryColumnVisible)
                {
                    maxArchiveEntryWidth = Math.Max(maxArchiveEntryWidth, item.IsArchiveEntry.ToString().Length);
                }
                if (columnSettings.IsArchiveFilePathColumnVisible)
                {
                    maxArchiveFilePathWidth = Math.Max(maxArchiveFilePathWidth, item.ArchiveFilePath.Length);
                }
                if (columnSettings.IsCompressedSizeColumnVisible && item.CompressedSize.HasValue)
                {
                    maxCompressedSizeWidth = Math.Max(maxCompressedSizeWidth, item.CompressedSize.Value.ToString().Length);
                }
                if (columnSettings.IsUncompressedSizeColumnVisible && item.UncompressedSize.HasValue)
                {
                    maxUncompressedSizeWidth = Math.Max(maxUncompressedSizeWidth, item.UncompressedSize.Value.ToString().Length);
                }
                if (columnSettings.IsFileSizeMBColumnVisible)
                {
                    // Учитываем форматирование с 2 знаками после запятой
                    maxFileSizeMBWidth = Math.Max(maxFileSizeMBWidth, item.FileSizeMB.ToString("F2").Length);
                }
                if (columnSettings.IsFileSizeBytesColumnVisible)
                {
                    maxFileSizeBytesWidth = Math.Max(maxFileSizeBytesWidth, item.FileSizeBytes.ToString().Length);
                }
            }

            // Устанавливаем минимальную ширину, если столбец видим, но все значения пустые/нулевые
            if (columnSettings.IsIndexColumnVisible)
            {
                maxIndexWidth = Math.Max(0, maxIndexWidth);
            }

            if (columnSettings.IsFileNameColumnVisible)
            {
                maxFileNameWidth = Math.Max(0, maxFileNameWidth); // Пример минимальной ширины
            }

            if (columnSettings.IsFolderRelativePathColumnVisible)
            {
                maxFolderRelativePathWidth = Math.Max(0, maxFolderRelativePathWidth);
            }

            if (columnSettings.IsPagesCountColumnVisible)
            {
                maxPagesCountWidth = Math.Max(0, maxPagesCountWidth);
            }

            if (columnSettings.IsFileExtensionColumnVisible)
            {
                maxFileExtensionWidth = Math.Max(0, maxFileExtensionWidth);
            }

            if (columnSettings.IsFileSHA256ColumnVisible)
            {
                maxFileSHA256Width = Math.Max(0, maxFileSHA256Width);
            }

            if (columnSettings.IsFilePathColumnVisible)
            {
                maxFilePathWidth = Math.Max(0, maxFilePathWidth);
            }

            if (columnSettings.IsFileRelativePathColumnVisible)
            {
                maxFileRelativePathWidth = Math.Max(0, maxFileRelativePathWidth);
            }

            if (columnSettings.IsArchiveFileColumnVisible)
            {
                maxArchiveFileWidth = Math.Max(0, maxArchiveFileWidth);
            }

            if (columnSettings.IsArchiveEntryColumnVisible)
            {
                maxArchiveEntryWidth = Math.Max(0, maxArchiveEntryWidth);
            }

            if (columnSettings.IsArchiveFilePathColumnVisible)
            {
                maxArchiveFilePathWidth = Math.Max(0, maxArchiveFilePathWidth);
            }

            if (columnSettings.IsCompressedSizeColumnVisible)
            {
                maxCompressedSizeWidth = Math.Max(0, maxCompressedSizeWidth);
            }

            if (columnSettings.IsUncompressedSizeColumnVisible)
            {
                maxUncompressedSizeWidth = Math.Max(0, maxUncompressedSizeWidth);
            }

            if (columnSettings.IsFileSizeMBColumnVisible)
            {
                maxFileSizeMBWidth = Math.Max(0, maxFileSizeMBWidth); // Пример, учитывая "F2"
            }

            if (columnSettings.IsFileSizeBytesColumnVisible)
            {
                maxFileSizeBytesWidth = Math.Max(0, maxFileSizeBytesWidth);
            }

            // --- Основной цикл: Формирование строки для каждого файла с выравниванием ---
            foreach (FileMetadata fileMetadata in itemsToProcess)
            {
                List<string> parts = [];

                if (columnSettings.IsIndexColumnVisible && fileMetadata.Index.HasValue)
                {
                    parts.Add($"Индекс: {fileMetadata.Index?.ToString().PadRight(maxIndexWidth)}");
                }
                if (columnSettings.IsFileNameColumnVisible)
                {
                    parts.Add($"Имя файла: {fileMetadata.FileName.PadRight(maxFileNameWidth)}");
                }
                if (columnSettings.IsFolderRelativePathColumnVisible)
                {
                    parts.Add($"Путь к файлу: {fileMetadata.FolderRelativePath.PadRight(maxFolderRelativePathWidth)}");
                }
                if (columnSettings.IsPagesCountColumnVisible)
                {
                    parts.Add($"Кол-во стр: {fileMetadata.PagesCount.ToString().PadRight(maxPagesCountWidth)}");
                }
                if (columnSettings.IsFileExtensionColumnVisible)
                {
                    parts.Add($"Расш: {fileMetadata.FileExtension.PadRight(maxFileExtensionWidth)}");
                }
                if (columnSettings.IsFileSHA256ColumnVisible)
                {
                    parts.Add($"SHA256: {fileMetadata.FileSHA256.PadRight(maxFileSHA256Width)}");
                }
                if (columnSettings.IsFilePathColumnVisible)
                {
                    parts.Add($"Полный путь: {fileMetadata.FilePath.PadRight(maxFilePathWidth)}");
                }
                if (columnSettings.IsFileRelativePathColumnVisible)
                {
                    parts.Add($"Отн. путь: {fileMetadata.FileRelativePath.PadRight(maxFileRelativePathWidth)}");
                }
                if (columnSettings.IsArchiveFileColumnVisible)
                {
                    parts.Add($"Архив: {fileMetadata.IsArchiveFile.ToString().PadRight(maxArchiveFileWidth)}");
                }
                if (columnSettings.IsArchiveEntryColumnVisible)
                {
                    parts.Add($"Зап.арх: {fileMetadata.IsArchiveEntry.ToString().PadRight(maxArchiveEntryWidth)}");
                }
                if (columnSettings.IsArchiveFilePathColumnVisible)
                {
                    parts.Add($"Путь арх: {fileMetadata.ArchiveFilePath.PadRight(maxArchiveFilePathWidth)}");
                }
                if (columnSettings.IsCompressedSizeColumnVisible && fileMetadata.CompressedSize.HasValue)
                {
                    parts.Add($"Сжатый: {fileMetadata.CompressedSize.Value.ToString().PadRight(maxCompressedSizeWidth)}");
                }
                if (columnSettings.IsUncompressedSizeColumnVisible && fileMetadata.UncompressedSize.HasValue)
                {
                    parts.Add($"Несжатый: {fileMetadata.UncompressedSize.Value.ToString().PadRight(maxUncompressedSizeWidth)}");
                }
                if (columnSettings.IsFileSizeMBColumnVisible)
                {
                    parts.Add($"Размер МБ: {fileMetadata.FileSizeMB.ToString("F2").PadRight(maxFileSizeMBWidth)}");
                }
                if (columnSettings.IsFileSizeBytesColumnVisible)
                {
                    parts.Add($"Размер байт: {fileMetadata.FileSizeBytes.ToString().PadRight(maxFileSizeBytesWidth)}");
                }

                // Соединяем видимые колонки для текущего файла в одну строку
                sb.AppendLine(string.Join("; ", parts));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Копирует данные выбранных файлов в формате TSV (Tab-Separated Values) в буфер обмена.
        /// </summary>
        /// <param name="selectedFiles">Коллекция выбранных файлов.</param>
        /// <param name="topLevel">Экземпляр TopLevel для доступа к буферу обмена.</param>
        public static async Task CopyAsTsvAsync(IList<FileMetadata> itemsToProcess, string selectedFolderPathMsg)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg("Нет выделенных элементов для копирования.");
                return;
            }

            StringBuilder sb = new();

            // Добавляем путь к выбранной папке
            sb.AppendLine(selectedFolderPathMsg);
            sb.AppendLine(); // Пустая строка для разделения

            // Заголовки столбцов
            sb.AppendLine("FileName\tFolderRelativePath\tPagesCount\tFileExtension\tFileSHA256");

            // Данные строк
            foreach (FileMetadata fileMetadata in itemsToProcess)
            {
                sb.AppendLine($"{fileMetadata.FileName}\t{fileMetadata.FolderRelativePath}\t{fileMetadata.PagesCount}\t{fileMetadata.FileExtension}\t{fileMetadata.FileSHA256}");
            }

            try
            {
                IClipboard? clipboard = _topLevel?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(sb.ToString());
                    GrowlsManager.ShowInfoMsg("Данные скопированы как TSV в буфер обмена.");
                }
                else
                {
                    GrowlsManager.ShowErrorMsg("Не удалось получить доступ к буферу обмена.");
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка копирования как TSV: {ex.Message}");
            }
        }
    }
}
