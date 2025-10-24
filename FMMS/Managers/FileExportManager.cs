using Avalonia.Platform.Storage;
using ClosedXML.Excel;
using FMMS.Items;
using FMMS.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class FileExportManager
    {
        public static async Task ExportToExcelAsync(ObservableCollection<FileMetadata> itemsToProcess, string selectedFolderPath)
        {
            if (itemsToProcess == null || itemsToProcess.Count == 0)
            {
                GrowlsManager.ShowInfoMsg("Нет выделенных элементов для экспорта.");
                return;
            }

            IStorageFile? result = await DialogsManager.SaveFileDialogAsync(
                suggestedFileName: Path.GetFileName(Path.GetFileName(selectedFolderPath.TrimEnd('\\', '/'))) + ".xlsx",
                allowedExtensions: ["*.xlsx"],
                title: "Сохранить как Excel");

            if (result != null)
            {
                string filePath = result.Path.LocalPath;

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    try
                    {
                        using XLWorkbook workbook = new();
                        IXLWorksheet worksheet = workbook.Worksheets.Add("FileMetadata");

                        // Заголовок с путем к папке
                        worksheet.Cell(1, 1).Value = "Путь к папке для анализа:";
                        worksheet.Cell(1, 2).Value = selectedFolderPath;

                        // Пустая строка
                        int currentRow = 3;

                        // Заголовки столбцов
                        worksheet.Cell(currentRow, 1).Value = "FileName";
                        worksheet.Cell(currentRow, 2).Value = "FolderRelativePath";
                        worksheet.Cell(currentRow, 3).Value = "PagesCount";
                        worksheet.Cell(currentRow, 4).Value = "FileExtension";
                        worksheet.Cell(currentRow, 5).Value = "FileSHA256";

                        // Закрепляем заголовки
                        worksheet.Row(currentRow).Style.Font.Bold = true;

                        currentRow++;

                        // Данные
                        foreach (FileMetadata fileMetadata in itemsToProcess)
                        {
                            worksheet.Cell(currentRow, 1).Value = fileMetadata.FileName;
                            worksheet.Cell(currentRow, 2).Value = fileMetadata.FolderRelativePath;
                            worksheet.Cell(currentRow, 3).Value = fileMetadata.PagesCount;
                            worksheet.Cell(currentRow, 4).Value = fileMetadata.FileExtension;
                            worksheet.Cell(currentRow, 5).Value = fileMetadata.FileSHA256;
                            currentRow++;
                        }

                        // Авто-ширина столбцов
                        worksheet.Columns().AdjustToContents();

                        // Сохраняем файл
                        await Task.Run(() => workbook.SaveAs(filePath));

                        GrowlsManager.ShowInfoMsg("Данные экспортированы в XLSX файл.");
                    }
                    catch (Exception ex)
                    {
                        GrowlsManager.ShowErrorMsg($"Ошибка экспорта в XLSX: {ex.Message}");
                    }
                }
            }
        }

        public static async Task ExportToTextAsync(ObservableCollection<FileMetadata> analyzedFiles, string selectedFolderPath)
        {
            IStorageFile? result = await DialogsManager.SaveFileDialogAsync(suggestedFileName: Path.GetFileName(Path.GetFileName(selectedFolderPath.TrimEnd('\\', '/'))) + ".txt", allowedExtensions: ["*.txt"]);

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

                    string compiledText = await ClipboardManager.CompileTextAsync(analyzedFiles, selectedFolderPath);

                    await File.WriteAllTextAsync(filePath, compiledText);
                }
            }
        }
    }
}
