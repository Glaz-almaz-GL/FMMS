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
        public long? CompressedSize { get; set; } // Сжатый размер (для записей внутри архива)
        public long? UncompressedSize { get; set; } // Не сжатый размер (для записей внутри архива)

        [ObservableProperty]
        private int? _index = null; // По умолчанию 0, будет обновлено позже

        public FileMetadata(string filePath, string fileRelativePath, string? folderRelativePath, string fileName, string fileExtension, string fileSHA256, int pagesCount, int? index = null, long fileSizeBytes = 0)
        {
            FilePath = filePath;
            FileRelativePath = fileRelativePath;
            FolderRelativePath = string.IsNullOrWhiteSpace(folderRelativePath) ? string.Empty : folderRelativePath;
            FileName = fileName;
            FileExtension = fileExtension;
            FileSHA256 = fileSHA256;
            PagesCount = pagesCount;
            Index = index;
            FileSizeBytes = fileSizeBytes;
        }

        public FileMetadata(string filePath, string fileRelativePath, string folderRelativePath, string fileName, string fileExtension, string fileSHA256, int pagesCount, bool isArchiveFile, bool isArchiveEntry, string archiveFilePath, long? compressedSize, long? uncompressedSize, int? index, long fileSizeBytes = 0)
        {
            FilePath = filePath;
            FileRelativePath = fileRelativePath;
            FolderRelativePath = string.IsNullOrWhiteSpace(folderRelativePath) ? string.Empty : folderRelativePath;
            FileName = fileName;
            FileExtension = fileExtension;
            FileSHA256 = fileSHA256;
            PagesCount = pagesCount;
            IsArchiveFile = isArchiveFile;
            IsArchiveEntry = isArchiveEntry;
            ArchiveFilePath = archiveFilePath;
            CompressedSize = compressedSize;
            UncompressedSize = uncompressedSize;
            Index = index;
            FileSizeBytes = fileSizeBytes;
        }

        public FileMetadata() { }
    }
}