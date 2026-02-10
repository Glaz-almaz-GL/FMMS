using Avalonia.Controls;
using Avalonia.Input.Platform;
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
                    GrowlsManager.ShowErrorMsg("Не удалось получить доступ к буферу обмена.");
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

            var sb = new StringBuilder();
            sb.AppendLine($"Путь до проанализированной папки: {selectedFolderPath}");
            sb.AppendLine();

            // Вычисляем максимальные длины значений для каждого активного столбца
            var maxWidths = CalculateMaxWidths(itemsToProcess, columnSettings);

            // Формируем строки для каждого элемента
            foreach (var item in itemsToProcess)
            {
                var lineParts = BuildLineParts(item, columnSettings, maxWidths);
                sb.AppendJoin("; ", lineParts).AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Вычисляет максимальную длину значений для каждого столбца среди всех элементов.
        /// </summary>
        private static Dictionary<ColumnSettingType, int> CalculateMaxWidths(
            IList<FileMetadata> itemsToProcess,
            ColumnSettingsItem columnSettings)
        {
            var maxWidths = new Dictionary<ColumnSettingType, int>();

            // Инициализируем словарь нулевыми значениями для видимых столбцов
            foreach (var settingType in Enum.GetValues<ColumnSettingType>())
            {
                if (IsColumnVisible(columnSettings, settingType))
                {
                    maxWidths[settingType] = 0;
                }
            }

            // Проходим по каждому элементу и обновляем максимальные длины
            foreach (var item in itemsToProcess)
            {
                // Итерируемся по всем типам столбцов, которые могут быть видимы
                foreach (var type in Enum.GetValues<ColumnSettingType>())
                {
                    // Проверяем, видим ли текущий столбец
                    if (IsColumnVisible(columnSettings, type))
                    {
                        // Получаем значение для этого столбца у текущего элемента
                        string value = GetValueForColumn(item, type);
                        // Вычисляем длину значения (если оно не null, используем длину строки, иначе 0)
                        int valueLength = value?.Length ?? 0;

                        // Обновляем максимальную ширину для этого типа столбца, если текущая длина больше
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
        private static bool IsColumnVisible(ColumnSettingsItem settings, ColumnSettingType type) => type switch
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

        /// <summary>
        /// Получает строковое значение для конкретного столбца элемента.
        /// </summary>
        private static string GetValueForColumn(FileMetadata item, ColumnSettingType type) => type switch
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

        /// <summary>
        /// Формирует список частей строки для одного элемента на основе настроек и ширин.
        /// </summary>
        private static List<string> BuildLineParts(
            FileMetadata item,
            ColumnSettingsItem columnSettings,
            Dictionary<ColumnSettingType, int> maxWidths)
        {
            var parts = new List<string>();
            var labels = GetLabels();

            // Итерируемся по всем возможным типам столбцов в определенном порядке
            foreach (var type in Enum.GetValues<ColumnSettingType>())
            {
                if (IsColumnVisible(columnSettings, type))
                {
                    string value = GetValueForColumn(item, type);
                    int maxWidth = maxWidths.TryGetValue(type, out int width) ? width : 0;
                    // Используем PadRight только если значение не пустое, иначе просто добавляем метку
                    string paddedValue = string.IsNullOrEmpty(value) ? "" : value.PadRight(maxWidth);
                    parts.Add($"{labels[type]}: {paddedValue}");
                }
            }

            return parts;
        }

        /// <summary>
        /// Возвращает словарь с метками для столбцов.
        /// </summary>
        private static Dictionary<ColumnSettingType, string> GetLabels() => new()
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

        /// <summary>
        /// Копирует данные выбранных файлов в формате TSV (Tab-Separated Values) в буфер обмена.
        /// </summary>
        public static async Task CopyAsTsvAsync(IList<FileMetadata> itemsToProcess, string selectedFolderPathMsg)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg("Нет выделенных элементов для копирования.");
                return;
            }

            var sb = new StringBuilder();

            // Добавляем путь к выбранной папке
            sb.AppendLine(selectedFolderPathMsg);
            sb.AppendLine(); // Пустая строка для разделения

            // Заголовки столбцов
            sb.AppendLine("FileName\tFolderRelativePath\tPagesCount\tFileExtension\tFileSHA256");

            // Данные строк
            foreach (var fileMetadata in itemsToProcess)
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

    /// <summary>
    /// Перечисление для типов столбцов, используемых в настройках и логике формирования строк.
    /// </summary>
    internal enum ColumnSettingType
    {
        Index,
        FileName,
        FolderRelativePath,
        PagesCount,
        FileExtension,
        FileSHA256,
        FilePath,
        FileRelativePath,
        ArchiveFile,
        ArchiveEntry,
        ArchiveFilePath,
        CompressedSize,
        UncompressedSize,
        FileSizeMB,
        FileSizeBytes,
    }
}