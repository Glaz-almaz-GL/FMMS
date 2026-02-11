using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace FMMS.Models
{
    public partial class FolderInfo : ObservableObject
    {
        [ObservableProperty]
        private string _relativePath = string.Empty;

        [ObservableProperty]
        private long _sizeInBytes;

        [ObservableProperty]
        private int _fileCount;

        // Новое свойство для абсолютного пути
        public string AbsolutePath { get; }

        public string SizeFormatted => FormatSize(SizeInBytes);

        public FolderInfo(string relativePath, long sizeInBytes, int fileCount, string absolutePath)
        {
            RelativePath = relativePath;
            SizeInBytes = sizeInBytes;
            FileCount = fileCount;
            AbsolutePath = absolutePath;
        }

        private static string FormatSize(long bytes)
        {
            string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
