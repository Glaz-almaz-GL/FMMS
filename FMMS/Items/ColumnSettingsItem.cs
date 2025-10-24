using CommunityToolkit.Mvvm.ComponentModel;

namespace FMMS.Items
{
    public partial class ColumnSettingsItem : ObservableObject
    {
        [ObservableProperty] private bool _isIndexColumnVisible = true;
        [ObservableProperty] private bool _isFileNameColumnVisible = true;
        [ObservableProperty] private bool _isFolderRelativePathColumnVisible = true;
        [ObservableProperty] private bool _isPagesCountColumnVisible = true;
        [ObservableProperty] private bool _isFileExtensionColumnVisible = true;
        [ObservableProperty] private bool _isFileSHA256ColumnVisible = true;
        [ObservableProperty] private bool _isFilePathColumnVisible = false;
        [ObservableProperty] private bool _isFileRelativePathColumnVisible = false;
        [ObservableProperty] private bool _isArchiveFileColumnVisible = false;
        [ObservableProperty] private bool _isArchiveEntryColumnVisible = false;
        [ObservableProperty] private bool _isArchiveFilePathColumnVisible = false;
        [ObservableProperty] private bool _isCompressedSizeColumnVisible = false;
        [ObservableProperty] private bool _isUncompressedSizeColumnVisible = false;
        [ObservableProperty] private bool _isFileSizeMBColumnVisible = false;
        [ObservableProperty] private bool _isFileSizeBytesColumnVisible = false;
    }
}
