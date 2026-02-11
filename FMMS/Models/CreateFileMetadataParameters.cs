using System;
using System.IO;
using System.Threading.Tasks;

namespace FMMS.Models
{
    /// <summary>
    /// Объект параметров для метода CreateFileMetadataAsync.
    /// </summary>
    public record CreateFileMetadataParameters(
        string FilePathOrEntryKey,
        string AnalyzedRootPath,
        bool IsArchive,
        bool IsEntry,
        string ArchivePath = "",
        long? CompressedSize = null,
        long? UncompressedSize = null,
        Func<string, Task<Stream?>>? ExtractStreamFunc = null
    );
}
