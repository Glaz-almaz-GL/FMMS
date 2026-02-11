namespace FMMS.Models
{
    /// <summary>
    /// Перечисление для типов столбцов, используемых в настройках и логике формирования строк.
    /// </summary>
    public enum ColumnSettingType
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

    public enum FolderColumnSettingType
    {
        RelativePath,
        SizeFormatted,
        FileCount
    }
}
