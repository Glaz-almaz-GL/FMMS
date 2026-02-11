using Avalonia.Controls;
using Avalonia.Input.Platform;
using FMMS.Helpers;
using FMMS.Items;
using FMMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class ClipboardManager
    {
        private const string ClipboardErrorMsg = "Не удалось получить доступ к буферу обмена.";
        private const string NoSelectedItemsToCopy = "Нет выделенных элементов для копирования.";
        private static TopLevel? _topLevel = null;

        public static void Initialize(TopLevel? topLevel)
        {
            _topLevel = topLevel;
        }

        /// <summary>
        /// Копирует пути выбранных файлов в буфер обмена в формате, определяемом настройками столбцов.
        /// </summary>
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
                    GrowlsManager.ShowErrorMsg(ClipboardErrorMsg);
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка копирования: {ex.Message}");
            }
        }

        /// <summary>
        /// Формирует строку с данными файлов, основываясь на настройках видимости столбцов.
        /// </summary>
        public static async Task<string> CompileTextAsync(
            IEnumerable<FileMetadata> itemsToProcess,
            string selectedFolderPath,
            ColumnSettingsItem? columnSettings = null)
        {
            columnSettings ??= SettingsManager.CurrentSettings.ColumnSettings;

            if (itemsToProcess?.Any() == false)
            {
                GrowlsManager.ShowWarningMsg(NoSelectedItemsToCopy);
                return string.Empty;
            }

            StringBuilder sb = new();
            sb.AppendLine($"Путь до проанализированной папки: {selectedFolderPath}");
            sb.AppendLine();

            // Вычисляем максимальные длины значений для каждого активного столбца
            Dictionary<ColumnSettingType, int> maxWidths = CalculateMaxWidths(itemsToProcess!, columnSettings);

            // Формируем строки для каждого элемента
            foreach (FileMetadata item in itemsToProcess!)
            {
                List<string> lineParts = BuildLineParts(item, columnSettings, maxWidths);
                sb.AppendJoin("; ", lineParts).AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Вычисляет максимальную длину значений для каждого столбца среди всех элементов.
        /// </summary>
        private static Dictionary<ColumnSettingType, int> CalculateMaxWidths(
            IEnumerable<FileMetadata> itemsToProcess,
            ColumnSettingsItem columnSettings)
        {
            Dictionary<ColumnSettingType, int> maxWidths = [];

            foreach (ColumnSettingType settingType in Enum.GetValues<ColumnSettingType>())
            {
                if (IsColumnVisible(columnSettings, settingType))
                {
                    maxWidths[settingType] = 0;
                }
            }

            foreach (FileMetadata item in itemsToProcess)
            {
                foreach (ColumnSettingType type in Enum.GetValues<ColumnSettingType>())
                {
                    if (IsColumnVisible(columnSettings, type))
                    {
                        string value = GetValueForColumn(item, type);
                        int valueLength = value?.Length ?? 0;

                        if (maxWidths.TryGetValue(type, out int value1))
                        {
                            maxWidths[type] = Math.Max(value1, valueLength);
                        }
                    }
                }
            }

            return maxWidths;
        }

        /// <summary>
        /// Проверяет, видим ли столбец по его типу.
        /// </summary>
        private static bool IsColumnVisible(ColumnSettingsItem settings, ColumnSettingType type)
        {
            return type switch
            {
                ColumnSettingType.Index => settings.IsIndexColumnVisible,
                ColumnSettingType.FileName => settings.IsFileNameColumnVisible,
                ColumnSettingType.FolderRelativePath => settings.IsFolderRelativePathColumnVisible,
                ColumnSettingType.PagesCount => settings.IsPagesCountColumnVisible,
                ColumnSettingType.FileExtension => settings.IsFileExtensionColumnVisible,
                ColumnSettingType.FileSHA256 => settings.IsFileSHA256ColumnVisible,
                ColumnSettingType.FilePath => settings.IsFilePathColumnVisible,
                ColumnSettingType.FileRelativePath => settings.IsFileRelativePathColumnVisible,
                ColumnSettingType.ArchiveFile => settings.IsArchiveFileColumnVisible,
                ColumnSettingType.ArchiveEntry => settings.IsArchiveEntryColumnVisible,
                ColumnSettingType.ArchiveFilePath => settings.IsArchiveFilePathColumnVisible,
                ColumnSettingType.CompressedSize => settings.IsCompressedSizeColumnVisible,
                ColumnSettingType.UncompressedSize => settings.IsUncompressedSizeColumnVisible,
                ColumnSettingType.FileSizeMB => settings.IsFileSizeMBColumnVisible,
                ColumnSettingType.FileSizeBytes => settings.IsFileSizeBytesColumnVisible,
                _ => false,
            };
        }

        /// <summary>
        /// Получает строковое значение для конкретного столбца элемента.
        /// </summary>
        private static string GetValueForColumn(FileMetadata item, ColumnSettingType type)
        {
            return type switch
            {
                ColumnSettingType.Index => item.Index?.ToString() ?? "",
                ColumnSettingType.FileName => item.FileName,
                ColumnSettingType.FolderRelativePath => item.FolderRelativePath,
                ColumnSettingType.PagesCount => item.PagesCount.ToString(),
                ColumnSettingType.FileExtension => item.FileExtension,
                ColumnSettingType.FileSHA256 => item.FileSHA256,
                ColumnSettingType.FilePath => item.FilePath,
                ColumnSettingType.FileRelativePath => item.FileRelativePath,
                ColumnSettingType.ArchiveFile => item.IsArchiveFile.ToString(),
                ColumnSettingType.ArchiveEntry => item.IsArchiveEntry.ToString(),
                ColumnSettingType.ArchiveFilePath => item.ArchiveFilePath,
                ColumnSettingType.CompressedSize => item.CompressedSizeBytes?.ToString() ?? "",
                ColumnSettingType.UncompressedSize => item.UncompressedSizeBytes?.ToString() ?? "",
                ColumnSettingType.FileSizeMB => item.FileSizeMB.ToString("F2"),
                ColumnSettingType.FileSizeBytes => item.FileSizeBytes.ToString(),
                _ => "",
            };
        }

        /// <summary>
        /// Формирует список частей строки для одного элемента на основе настроек и ширин.
        /// </summary>
        private static List<string> BuildLineParts(
            FileMetadata item,
            ColumnSettingsItem columnSettings,
            Dictionary<ColumnSettingType, int> maxWidths)
        {
            List<string> parts = [];
            Dictionary<ColumnSettingType, string> labels = GetLabels();

            foreach (ColumnSettingType type in Enum.GetValues<ColumnSettingType>())
            {
                if (IsColumnVisible(columnSettings, type))
                {
                    string value = GetValueForColumn(item, type);
                    int maxWidth = maxWidths.TryGetValue(type, out int width) ? width : 0;
                    string paddedValue = string.IsNullOrEmpty(value) ? "" : value.PadRight(maxWidth);
                    parts.Add($"{labels[type]}: {paddedValue}");
                }
            }

            return parts;
        }

        /// <summary>
        /// Возвращает словарь с метками для столбцов.
        /// </summary>
        private static Dictionary<ColumnSettingType, string> GetLabels()
        {
            return new()
            {
                { ColumnSettingType.Index, "Индекс" },
                { ColumnSettingType.FileName, "Имя файла" },
                { ColumnSettingType.FolderRelativePath, "Путь к файлу" },
                { ColumnSettingType.PagesCount, "Кол-во стр" },
                { ColumnSettingType.FileExtension, "Расш" },
                { ColumnSettingType.FileSHA256, "SHA256" },
                { ColumnSettingType.FilePath, "Полный путь" },
                { ColumnSettingType.FileRelativePath, "Отн. путь" },
                { ColumnSettingType.ArchiveFile, "Архив" },
                { ColumnSettingType.ArchiveEntry, "Зап.арх" },
                { ColumnSettingType.ArchiveFilePath, "Путь арх" },
                { ColumnSettingType.CompressedSize, "Сжатый" },
                { ColumnSettingType.UncompressedSize, "Несжатый" },
                { ColumnSettingType.FileSizeMB, "Размер МБ" },
                { ColumnSettingType.FileSizeBytes, "Размер байт" },
            };
        }

        /// <summary>
        /// Копирует данные выбранных файлов в формате TSV (Tab-Separated Values) в буфер обмена.
        /// </summary>
        public static async Task CopyAsTsvAsync(IList<FileMetadata> itemsToProcess, string selectedFolderPathMsg)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg(NoSelectedItemsToCopy);
                return;
            }

            StringBuilder sb = new();

            // Добавляем путь к выбранной папке
            sb.AppendLine(selectedFolderPathMsg);
            sb.AppendLine();

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
                }
                else
                {
                    GrowlsManager.ShowErrorMsg(ClipboardErrorMsg);
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка копирования как TSV: {ex.Message}");
            }
        }

        #region FolderInfo Methods

        /// <summary>
        /// Копирует пути выбранных папок в буфер обмена в формате TSV.
        /// </summary>
        public static async Task CopyFoldersAsTsvAsync(IList<FolderInfo> itemsToProcess, string selectedFolderPathMsg)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg(NoSelectedItemsToCopy);
                return;
            }

            // Сортируем перед копированием
            var sortedItems = SortHelper.SortFolderInfosByRelativePath(itemsToProcess);

            StringBuilder sb = new();

            // Добавляем путь к выбранной папке
            sb.AppendLine($"Путь до проанализированной папки: {selectedFolderPathMsg}");
            sb.AppendLine();

            // Заголовки столбцов
            sb.AppendLine("RelativePath\tSizeFormatted\tFileCount");

            // Данные строк
            foreach (FolderInfo folderInfo in sortedItems)
            {
                sb.AppendLine($"{folderInfo.RelativePath}\t{folderInfo.SizeFormatted}\t{folderInfo.FileCount}");
            }

            try
            {
                IClipboard? clipboard = _topLevel?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(sb.ToString());
                }
                else
                {
                    GrowlsManager.ShowErrorMsg(ClipboardErrorMsg);
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка копирования папок как TSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Копирует пути выбранных папок в буфер обмена в формате, определяемом настройками столбцов (аналогично файлам).
        /// </summary>
        public static async Task CopyFoldersAsTextAsync(IList<FolderInfo> itemsToProcess, string selectedFolderPathMsg)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg(NoSelectedItemsToCopy);
                return;
            }

            // Сортируем перед копированием
            var sortedItems = SortHelper.SortFolderInfosByRelativePath(itemsToProcess);

            StringBuilder sb = new();
            sb.AppendLine($"Путь до проанализированной папки: {selectedFolderPathMsg}");
            sb.AppendLine();

            // Вычисляем максимальные длины значений для каждого активного столбца
            Dictionary<FolderColumnSettingType, int> maxWidths = CalculateMaxWidthsForFolders(sortedItems);

            // Формируем строки для каждого элемента
            foreach (FolderInfo item in sortedItems)
            {
                List<string> lineParts = BuildLinePartsForFolder(item, maxWidths);
                sb.AppendJoin("; ", lineParts).AppendLine();
            }

            try
            {
                IClipboard? clipboard = _topLevel?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(sb.ToString());
                }
                else
                {
                    GrowlsManager.ShowErrorMsg(ClipboardErrorMsg);
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка копирования данных папок: {ex.Message}");
            }
        }

        /// <summary>
        /// Вычисляет максимальную длину значений для каждого столбца папок (относительный путь, размер, файлы).
        /// </summary>
        public static Dictionary<FolderColumnSettingType, int> CalculateMaxWidthsForFolders(
            IList<FolderInfo> itemsToProcess)
        {
            Dictionary<FolderColumnSettingType, int> maxWidths = [];

            // Инициализируем словарь нулевыми значениями для всех столбцов FolderInfo
            // Предполагаем, что все столбцы FolderInfo всегда учитываются (в отличие от FileMetadata с настройками видимости)
            foreach (FolderColumnSettingType settingType in Enum.GetValues<FolderColumnSettingType>())
            {
                maxWidths[settingType] = 0;
            }

            // Проходим по каждому элементу и обновляем максимальные длины
            foreach (FolderInfo item in itemsToProcess)
            {
                // Итерируемся по всем типам столбцов FolderInfo
                foreach (FolderColumnSettingType type in Enum.GetValues<FolderColumnSettingType>())
                {
                    // Получаем значение для этого столбца у текущего элемента
                    string value = GetValueForFolderColumn(item, type);
                    // Вычисляем длину значения (если оно не null, используем длину строки, иначе 0)
                    int valueLength = value?.Length ?? 0;

                    // Обновляем максимальную ширину для этого типа столбца, если текущая длина больше
                    if (maxWidths.TryGetValue(type, out int currentValue))
                    {
                        maxWidths[type] = Math.Max(currentValue, valueLength);
                    }
                }
            }

            return maxWidths;
        }

        /// <summary>
        /// Получает строковое значение для конкретного столбца элемента FolderInfo.
        /// </summary>
        private static string GetValueForFolderColumn(FolderInfo item, FolderColumnSettingType type)
        {
            return type switch
            {
                FolderColumnSettingType.RelativePath => item.RelativePath,
                FolderColumnSettingType.SizeFormatted => item.SizeFormatted,
                FolderColumnSettingType.FileCount => item.FileCount.ToString(),
                _ => "",
            };
        }

        /// <summary>
        /// Формирует список частей строки для одного элемента FolderInfo на основе ширин.
        /// </summary>
        public static List<string> BuildLinePartsForFolder(
            FolderInfo item,
            Dictionary<FolderColumnSettingType, int> maxWidths)
        {
            List<string> parts = [];
            Dictionary<FolderColumnSettingType, string> labels = GetFolderLabels();

            // Итерируемся по всем возможным типам столбцов FolderInfo в определенном порядке
            foreach (FolderColumnSettingType type in Enum.GetValues<FolderColumnSettingType>())
            {
                string value = GetValueForFolderColumn(item, type);
                int maxWidth = maxWidths.TryGetValue(type, out int width) ? width : 0;
                // Используем PadRight только если значение не пустое, иначе просто добавляем метку
                string paddedValue = string.IsNullOrEmpty(value) ? "" : value.PadRight(maxWidth);
                parts.Add($"{labels[type]}: {paddedValue}");
            }

            return parts;
        }

        /// <summary>
        /// Возвращает словарь с метками для столбцов папок.
        /// </summary>
        private static Dictionary<FolderColumnSettingType, string> GetFolderLabels()
        {
            return new()
            {
                { FolderColumnSettingType.RelativePath, "Отн. путь" },
                { FolderColumnSettingType.SizeFormatted, "Размер" },
                { FolderColumnSettingType.FileCount, "Файлов" },
            };
        }

        #endregion
    }
}