using CommunityToolkit.Mvvm.ComponentModel;

namespace FMMS.Items
{
    public partial class FileMetadata : ObservableObject
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileRelativePath { get; set; } = string.Empty;
        public string FolderRelativePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileNameWithoutExtension => FileName.Replace(FileExtension, string.Empty);
        public string FileExtension { get; set; } = string.Empty;
        public string FileSHA256 { get; set; } = string.Empty;
        public int PagesCount { get; set; } = 0;

        // Новый параметр для размера файла
        public long FileSizeBytes { get; set; } = 0;
        public double FileSizeMB
        {
            get
            {
                if (FileSizeBytes <= 0)
                {
                    return 0.0;
                }

                return (double)FileSizeBytes / (1024 * 1024); // Байты в МБ
            }
        }

        public bool IsArchiveFile { get; set; } = false; // Является ли файл архивом
        public bool IsArchiveEntry { get; set; } = false; // Является ли запись файлом внутри архива
        public string ArchiveFilePath { get; set; } = string.Empty; // Путь к родительскому архиву (для записей внутри)
        public long? CompressedSizeBytes { get; set; } // Сжатый размер (для записей внутри архива)
        public double? CompressedSizeMB => CompressedSizeBytes is null or <= 0 ? (double?)0 : (double)CompressedSizeBytes / (1024 * 1024);

        public long? UncompressedSizeBytes { get; set; } // Не сжатый размер (для записей внутри архива)
        public double? UncompressedSizeMB => UncompressedSizeBytes is null or <= 0 ? (double?)0 : (double)UncompressedSizeBytes / (1024 * 1024);

        [ObservableProperty]
        private int? _index = null; // По умолчанию 0, будет обновлено позже
    }
}