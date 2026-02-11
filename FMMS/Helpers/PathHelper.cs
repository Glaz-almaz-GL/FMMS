using Avalonia.Platform.Storage;
using FMMS.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FMMS.Helpers
{
    public static class PathHelper
    {
        public static string? ExtractPathFromStorageFolder(IStorageFolder storageFolder)
        {
            if (storageFolder.Path is not Uri selectedUri)
            {
                throw new ArgumentException($"Тип возвращенного пути не поддерживается ({storageFolder.Path?.GetType().FullName}): {storageFolder.Path}");
            }

            string originalUriString = selectedUri.OriginalString;

            // Случай 1: Относительный URI, но строка является корневым путем
            if (!selectedUri.IsAbsoluteUri && Path.IsPathRooted(originalUriString))
            {
                return originalUriString;
            }

            // Случай 2: Относительный URI с file:// префиксом
            if (!selectedUri.IsAbsoluteUri)
            {
                return ExtractPathFromRelativeUri(originalUriString);
            }

            // Случай 3: Абсолютный URI с file:// схемой
            if (selectedUri.Scheme == Uri.UriSchemeFile)
            {
                return ExtractPathFromAbsoluteFileUri(selectedUri, originalUriString);
            }

            // Случай 4: Неизвестная схема
            GrowlsManager.ShowErrorMsg($"Выбранный URI не является локальным файлом: {selectedUri}");
            return null;
        }

        private static string? ExtractPathFromRelativeUri(string originalUriString)
        {
            if (originalUriString.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                string potentialPath = originalUriString[7..];
                if (Path.IsPathRooted(potentialPath))
                {
                    return potentialPath;
                }
                else
                {
                    GrowlsManager.ShowErrorMsg($"Некорректный относительный URI (не абсолютный путь): {originalUriString}");
                }
            }
            else
            {
                GrowlsManager.ShowErrorMsg($"Выбранный относительный URI не поддерживается: {originalUriString}");
            }

            return null;
        }

        private static string? ExtractPathFromAbsoluteFileUri(Uri selectedUri, string originalUriString)
        {
            try
            {
                return selectedUri.LocalPath;
            }
            catch (InvalidOperationException ex)
            {
                if (originalUriString.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                {
                    string pathWithoutPrefix = originalUriString[8..];
                    if (Path.IsPathRooted(pathWithoutPrefix))
                    {
                        return pathWithoutPrefix;
                    }
                    else
                    {
                        GrowlsManager.ShowErrorMsg(ex, $"Некорректный путь после удаления префикса 'file:///' для URI: {originalUriString}");
                    }
                }
                else
                {
                    GrowlsManager.ShowErrorMsg(ex, $"Неожиданный формат корректного absolute file URI: {originalUriString}");
                }

                return null;
            }
        }

        public static async Task<string?> GetSelectedFolderPathAsync()
        {
            IStorageFolder? result = await DialogsManager.OpenSingleFolderDialogAsync("Выберите папку для анализа");
            if (result == null)
            {
                return null;
            }

            if (result.Path is not Uri selectedUri)
            {
                GrowlsManager.ShowErrorMsg($"Тип возвращенного пути не поддерживается ({result.Path?.GetType().FullName}): {result.Path}");
                return null;
            }

            string? extractedPath = ExtractPathFromUri(selectedUri);
            return string.IsNullOrEmpty(extractedPath) ? null : extractedPath;
        }

        public static string? ExtractPathFromUri(Uri selectedUri)
        {
            string originalUriString = selectedUri.OriginalString;
            System.Diagnostics.Debug.WriteLine($"OriginalUriString: '{originalUriString}'");
            System.Diagnostics.Debug.WriteLine($"IsAbsoluteUri: {selectedUri.IsAbsoluteUri}");

            // Случай 1: Относительный URI, но строка является корневым путем
            if (!selectedUri.IsAbsoluteUri && Path.IsPathRooted(originalUriString))
            {
                System.Diagnostics.Debug.WriteLine($"Используем OriginalString как путь: {originalUriString}");
                return originalUriString;
            }

            // Случай 2: Относительный URI с file:// префиксом
            if (!selectedUri.IsAbsoluteUri)
            {
                return ExtractPathFromRelativeUri(originalUriString);
            }

            // Случай 3: Абсолютный URI с file:// схемой
            if (selectedUri.Scheme == Uri.UriSchemeFile)
            {
                return ExtractPathFromAbsoluteFileUri(selectedUri, originalUriString);
            }

            // Случай 4: Неизвестная схема
            GrowlsManager.ShowErrorMsg($"Выбранный URI не является локальным файлом: {selectedUri}");
            return null;
        }

        public static bool ValidateFolderPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                GrowlsManager.ShowWarningMsg("Не удалось извлечь путь из выбранной папки.");
                return false;
            }

            if (!Directory.Exists(path))
            {
                GrowlsManager.ShowWarningMsg($"Папка не существует: {path}");
                return false;
            }

            return true;
        }

        public static string GetRelativePath(string fullPath, string basePath)
        {
            Uri baseUri = new(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            Uri fullUri = new(fullPath);
            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}