using Avalonia.Input;
using Avalonia.Platform.Storage;
using FMMS.Models;
using System.IO;

namespace FMMS.Managers
{
    public static class DragDropManager
    {
        /// <summary>
        /// Обработчик события Drop.
        /// </summary>
        /// <param name="e">Аргументы события Drop.</param>
        /// <returns>Путь к папке, если была сброшена папка, иначе null.</returns>
        public static (bool? isDir, string? filePath) HandleDrop(DragEventArgs e)
        {
            IStorageItem? file = e.DataTransfer.TryGetFile();
            if (file != null)
            {
                string filePath = file.Path.LocalPath;

                return ProcessDroppedFile(filePath);
            }
            else
            {
                GrowlsManager.ShowErrorMsg($"Файл не существует");
            }

            return (null, null); // Ничего подходящего не было сброшено
        }

        /// <summary>
        /// Обрабатывает сброшенные пути (файлы или папки).
        /// </summary>
        /// <param name="filePaths">Список сброшенных путей.</param>
        /// <returns>Путь к папке, если была сброшена папка, иначе null.</returns>
        private static (bool? isDir, string? filePath) ProcessDroppedFile(string filePath)
        {
            if (Directory.Exists(filePath))
            {
                return (true, filePath);
            }
            else if (File.Exists(filePath))
            {
                return (false, filePath);
            }

            return (null, null); // Не было сброшено ни одной папки
        }
    }
}