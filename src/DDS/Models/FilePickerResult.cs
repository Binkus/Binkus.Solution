using Avalonia.Platform.Storage;

namespace DDS.Models;

public struct FilePickerResult
{
    public FilePickerResult()
    {
    }

    public bool Exists => StorageFiles is not null;

    public string ContentType { get; init; } = "";

    public string FileName { get; set; } = "";

    public string FullPath { get; set; } = "";
    
    public IReadOnlyList<IStorageFile>? StorageFiles { get; init; } = null;
}