using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Huskui.Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FMMS.Models
{
    public static class DialogsManager
    {
        private static TopLevel? _topLevel;
        private static AppWindow? _appWindow;

        // Статический метод для инициализации
        public static void Initialize(TopLevel topLevel, AppWindow appWindow)
        {
            _topLevel = topLevel;
            _appWindow = appWindow;
        }

        #region Методы диалога сообщений

        public static async Task<bool?> ShowMsgDialogAsync(
            string message,
            string title,
            bool showButtons = false,
            string primaryButtonText = "",
            string secondaryButtonText = "")
        {
            if (_appWindow == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("Message cannot be null or empty", nameof(message));
            }

            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("Title cannot be null or empty", nameof(title));
            }

            YesNoDialog dialog = new()
            {
                Title = title,
                Content = message,
                PrimaryText = showButtons ? primaryButtonText : "Да",
                SecondaryText = secondaryButtonText ?? "Отмена",
                IsPrimaryButtonVisible = showButtons
            };

            _appWindow.PopDialog(dialog);
            return await dialog.CompletionSource.Task;
        }

        #endregion

        #region Методы диалога файлов

        public static async Task<IStorageFolder?> OpenSingleFolderDialogAsync(string title)
        {
            if (_topLevel == null)
            {
                return null;
            }

            FolderPickerOpenOptions options = CreateFolderPickerOptions(title, false);
            IReadOnlyList<IStorageFolder> folder = await _topLevel.StorageProvider.OpenFolderPickerAsync(options);

            return folder?.Count > 0 ? folder[0] : null;
        }

        public static async Task<IStorageFile?> OpenSingleFileDialogAsync(string title, IEnumerable<string>? allowedExtensions = null)
        {
            if (_topLevel == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be null or empty", nameof(title));
            }

            FilePickerOpenOptions options = CreateFilePickerOptions(title, allowedExtensions, false);
            IReadOnlyList<IStorageFile> files = await _topLevel.StorageProvider.OpenFilePickerAsync(options);

            return files.Count > 0 ? files[0] : null;
        }

        public static async Task<IReadOnlyList<IStorageFile>?> OpenMultipleFilesDialogAsync(string title, IEnumerable<string>? allowedExtensions = null)
        {
            if (_topLevel == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be null or empty", nameof(title));
            }

            FilePickerOpenOptions options = CreateFilePickerOptions(title, allowedExtensions, true);
            IReadOnlyList<IStorageFile> files = await _topLevel.StorageProvider.OpenFilePickerAsync(options);

            return files.Count > 0 ? files : null;
        }

        public static async Task<IStorageFile?> SaveFileDialogAsync(string title = "Сохранить файл", string? suggestedFileName = null, IEnumerable<string>? allowedExtensions = null)
        {
            if (_topLevel == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be null or empty", nameof(title));
            }

            FilePickerSaveOptions options = new()
            {
                Title = title,
                SuggestedFileName = suggestedFileName
            };

            if (allowedExtensions?.Any() == true)
            {
                options.FileTypeChoices =
                [
                    new FilePickerFileType(title)
            {
                Patterns = [.. allowedExtensions]
            }
                ];
            }

            IStorageFile? file = await _topLevel.StorageProvider.SaveFilePickerAsync(options);
            return file;
        }

        #endregion

        #region Удобные методы диалога файлов

        public static async Task<IStorageFile?> OpenTextFileDialogAsync(string title = "Выберите текстовый файл")
        {
            string[] extensions = ["*.txt", "*.str", "*.csv"];
            return await OpenSingleFileDialogAsync(title, extensions);
        }

        public static async Task<IReadOnlyList<IStorageFile>?> OpenMultipleTextFilesDialogAsync(string title = "Выберите текстовые файлы")
        {
            string[] extensions = ["*.txt", "*.str", "*.csv"];
            return await OpenMultipleFilesDialogAsync(title, extensions);
        }

        #endregion

        #region Приватные помощники

        private static FilePickerOpenOptions CreateFilePickerOptions(
            string title,
            IEnumerable<string>? allowedExtensions,
            bool allowMultiple)
        {
            FilePickerOpenOptions options = new()
            {
                Title = title,
                AllowMultiple = allowMultiple
            };

            if (allowedExtensions?.Any() == true)
            {
                options.FileTypeFilter =
                [
                    new FilePickerFileType(title)
                    {
                        Patterns = [.. allowedExtensions]
                    }
                ];
            }

            return options;
        }

        private static FolderPickerOpenOptions CreateFolderPickerOptions(string title, bool allowMultiple)
        {
            FolderPickerOpenOptions options = new()
            {
                Title = title,
                AllowMultiple = allowMultiple
            };

            return options;
        }

        #endregion
    }
}