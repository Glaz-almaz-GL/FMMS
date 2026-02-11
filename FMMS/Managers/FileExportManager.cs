using Avalonia.Platform.Storage;
using ClosedXML.Excel;
using FMMS.Helpers;
using FMMS.Items;
using FMMS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class FileExportManager
    {
        // --- Общие методы для Excel ---

        public static async Task ExportToExcelAsync(ObservableCollection<FileMetadata> itemsToProcess, string selectedFolderPath)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg("Нет выделенных элементов для экспорта.");
                return;
            }

            string suggestedFileName = Path.GetFileName(selectedFolderPath.TrimEnd('\\', '/')) + ".xlsx";
            await ExportToExcelGenericAsync(itemsToProcess, selectedFolderPath, suggestedFileName, "FileMetadata", WriteFileMetadataToWorksheet);
        }

        public static async Task ExportFoldersToExcelAsync(ObservableCollection<FolderInfo> itemsToProcess, string selectedFolderPath)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg("Нет выделенных элементов для экспорта.");
                return;
            }

            // Сортируем перед экспортом
            var sortedItems = SortHelper.SortFolderInfosByRelativePath(itemsToProcess).AsEnumerable();

            string suggestedFileName = Path.GetFileName(selectedFolderPath.TrimEnd('\\', '/')) + "_folders.xlsx";
            await ExportToExcelGenericAsync(sortedItems, selectedFolderPath, suggestedFileName, "FolderInfo", WriteFolderInfoToWorksheet);
        }

        // --- Общий метод для Excel ---
        private static async Task ExportToExcelGenericAsync<T>(
            IEnumerable<T> itemsToProcess,
            string selectedFolderPath,
            string suggestedFileName,
            string worksheetName,
            Action<IXLWorksheet, T, int> writeRowAction)
        {
            IStorageFile? result = await DialogsManager.SaveFileDialogAsync(
                title: "Сохранить как Excel",
                suggestedFileName: suggestedFileName,
                allowedExtensions: ["*.xlsx"]);

            if (result != null)
            {
                string filePath = result.Path.LocalPath;

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    try
                    {
                        using XLWorkbook workbook = new();
                        IXLWorksheet worksheet = workbook.Worksheets.Add(worksheetName);

                        // Заголовок с путем к папке
                        worksheet.Cell(1, 1).Value = "Путь к папке для анализа:";
                        worksheet.Cell(1, 2).Value = selectedFolderPath;

                        // Пустая строка
                        int currentRow = 3;

                        // Заголовки столбцов (зависит от типа T)
                        WriteHeaders(worksheet, currentRow, typeof(T));
                        worksheet.Row(currentRow).Style.Font.Bold = true;
                        currentRow++;

                        // Данные
                        foreach (T item in itemsToProcess)
                        {
                            writeRowAction(worksheet, item, currentRow);
                            currentRow++;
                        }

                        // Авто-ширина столбцов
                        worksheet.Columns().AdjustToContents();

                        // Сохраняем файл
                        await Task.Run(() => workbook.SaveAs(filePath));

                        GrowlsManager.ShowInfoMsg($"Данные экспортированы в XLSX файл: {Path.GetFileName(filePath)}.");
                    }
                    catch (Exception ex)
                    {
                        GrowlsManager.ShowErrorMsg($"Ошибка экспорта в XLSX: {ex.Message}");
                    }
                }
            }
        }

        // --- Конкретные методы для записи строк Excel ---
        private static void WriteHeaders(IXLWorksheet worksheet, int row, Type itemType)
        {
            if (itemType == typeof(FileMetadata))
            {
                worksheet.Cell(row, 1).Value = "FileName";
                worksheet.Cell(row, 2).Value = "FolderRelativePath";
                worksheet.Cell(row, 3).Value = "PagesCount";
                worksheet.Cell(row, 4).Value = "FileExtension";
                worksheet.Cell(row, 5).Value = "FileSHA256";
            }
            else if (itemType == typeof(FolderInfo))
            {
                worksheet.Cell(row, 1).Value = "RelativePath";
                worksheet.Cell(row, 2).Value = "SizeFormatted";
                worksheet.Cell(row, 3).Value = "FileSizeInBytes";
                worksheet.Cell(row, 4).Value = "FileCount";
            }
            // Добавьте другие типы при необходимости
        }

        private static void WriteFileMetadataToWorksheet(IXLWorksheet ws, FileMetadata item, int row)
        {
            ws.Cell(row, 1).Value = item.FileName;
            ws.Cell(row, 2).Value = item.FolderRelativePath;
            ws.Cell(row, 3).Value = item.PagesCount;
            ws.Cell(row, 4).Value = item.FileExtension;
            ws.Cell(row, 5).Value = item.FileSHA256;
        }

        private static void WriteFolderInfoToWorksheet(IXLWorksheet ws, FolderInfo item, int row)
        {
            ws.Cell(row, 1).Value = item.RelativePath;
            ws.Cell(row, 2).Value = item.SizeFormatted;
            ws.Cell(row, 3).Value = item.SizeInBytes;
            ws.Cell(row, 4).Value = item.FileCount;
        }

        // --- Общие методы для текста ---

        public static async Task ExportToTextAsync(ObservableCollection<FileMetadata> analyzedFiles, string selectedFolderPath)
        {
            string suggestedFileName = Path.GetFileName(selectedFolderPath.TrimEnd('\\', '/')) + ".txt";
            await ExportToTextGenericAsync(analyzedFiles, selectedFolderPath, suggestedFileName, async (items, path) => await ClipboardManager.CompileTextAsync(items, path));
        }

        public static async Task ExportFoldersToTextAsync(ObservableCollection<FolderInfo> itemsToProcess, string selectedFolderPath)
        {
            string suggestedFileName = Path.GetFileName(selectedFolderPath.TrimEnd('\\', '/')) + "_folders.txt";
            // Сортируем перед экспортом
            var sortedItems = SortHelper.SortFolderInfosByRelativePath(itemsToProcess);
            await ExportToTextGenericAsync(sortedItems, selectedFolderPath, suggestedFileName, CompileFoldersTextAsync);
        }

        // --- Общий метод для текста ---
        private static async Task ExportToTextGenericAsync<T>(
            IEnumerable<T> itemsToProcess,
            string selectedFolderPath,
            string suggestedFileName,
            Func<IEnumerable<T>, string, Task<string>> compileTextFunction)
        {
            var itemList = itemsToProcess.ToList();
            if (itemList == null || itemList.Count == 0)
            {
                GrowlsManager.ShowInfoMsg("Нет элементов для экспорта.");
                return;
            }

            IStorageFile? result = await DialogsManager.SaveFileDialogAsync(
                suggestedFileName: suggestedFileName,
                allowedExtensions: ["*.txt"]);

            if (result != null)
            {
                string filePath = result.Path.LocalPath;

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                    if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                    {
                        GrowlsManager.ShowErrorMsg("Имя файла не может быть пустым.");
                        return;
                    }

                    string compiledText = await compileTextFunction(itemsToProcess, selectedFolderPath);

                    await File.WriteAllTextAsync(filePath, compiledText);
                    GrowlsManager.ShowInfoMsg($"Данные экспортированы в TXT файл: {Path.GetFileName(filePath)}.");
                }
            }
        }

        // --- Конкретный метод для формирования текста FolderInfo ---
        private static async Task<string> CompileFoldersTextAsync(
            IEnumerable<FolderInfo> itemsToProcess,
            string selectedFolderPath)
        {
            var itemList = itemsToProcess.ToList();
            if (itemList == null || itemList.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            sb.AppendLine($"Путь до проанализированной папки: {selectedFolderPath}");
            sb.AppendLine();

            // Вычисляем максимальные длины значений для каждого столбца
            Dictionary<FolderColumnSettingType, int> maxWidths = ClipboardManager.CalculateMaxWidthsForFolders(itemList);

            // Формируем строки для каждого элемента
            foreach (FolderInfo item in itemList)
            {
                List<string> lineParts = ClipboardManager.BuildLinePartsForFolder(item, maxWidths);
                sb.AppendJoin("; ", lineParts).AppendLine();
            }

            return sb.ToString();
        }
    }
}