using FMMS.Models;
using Shell32;
using System;
using System.Diagnostics;
using System.IO;

namespace FMMS.Managers
{
    /// <summary>
    /// Статический класс, содержащий методы для выполнения действий из контекстного меню файлов.
    /// </summary>
    public static class FileMetadataManager
    {
        private static Shell? _shell = null;

        /// <summary>
        /// Открывает выбранный файл с помощью ассоциированной программы.
        /// </summary>
        /// <param name="selectedFiles">Коллекция выбранных файлов. Используется только первый элемент.</param>
        public static void OpenFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                GrowlsManager.ShowInfoMsg("Нет выделенных элементов для открытия.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Используем системный ассоциированный обработчик
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть файл: {ex.Message}");
            }
        }

        /// <summary>
        /// Открывает папку, содержащую выбранный файл, в проводнике.
        /// </summary>
        /// <param name="selectedFiles">Коллекция выбранных файлов. Используется только первый элемент.</param>
        public static void OpenContainingFolder(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                GrowlsManager.ShowInfoMsg("Нет выделенных элементов для открытия папки.");
                return;
            }

            try
            {
                // Проверяем, является ли путь файлом или папкой
                FileAttributes attributes = File.GetAttributes(filePath);
                string targetPath;
                string argument;

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    // Если это папка, открываем её напрямую
                    targetPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "explorer.exe" : "xdg-open";
                    argument = filePath;
                }
                else
                {
                    // Если это файл, открываем папку с выделением файла
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        // Для Windows используем explorer.exe с аргументом /select
                        targetPath = "explorer.exe";
                        argument = $"/select,\"{filePath}\"";
                    }
                    else
                    {
                        // Для Unix-подобных систем (Linux, macOS) открываем папку
                        // Выделение возможно, но требует сторонних утилит (например, AppleScript на macOS)
                        targetPath = "xdg-open"; // Или "open" на macOS
                        argument = Path.GetDirectoryName(filePath) ?? string.Empty;
                    }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = targetPath,
                    Arguments = argument,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Не удалось открыть папку: {ex.Message}");
            }
        }

        /// <summary>
        /// Показывает диалог свойств для выбранного файла (только Windows).
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        public static void ShowFileProperties(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                GrowlsManager.ShowInfoMsg("Нет файла для отображения свойств.");
                return;
            }

            if (!File.Exists(filePath))
            {
                GrowlsManager.ShowWarningMsg("Файл не существует.");
                return;
            }

            try
            {
                _shell ??= new Shell();

                string? directoryPath = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileName(filePath);

                Folder folder = _shell.NameSpace(directoryPath);
                FolderItem folderItem = folder.ParseName(fileName);

                if (folderItem != null)
                {
                    folderItem.InvokeVerb("properties");
                }
                else
                {
                    GrowlsManager.ShowWarningMsg("Не удалось получить доступ к файлу.");
                }
            }
            catch (Exception ex)
            {
                GrowlsManager.ShowErrorMsg($"Ошибка при открытии свойств файла: {ex.Message}");
            }
        }
    }
}
