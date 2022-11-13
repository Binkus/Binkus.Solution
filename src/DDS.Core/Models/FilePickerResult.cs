namespace DDS.Avalonia.Models;

public struct FilePickerResult
{
    public FilePickerResult()
    {
    }

    public bool Exists => StorageFiles is not null;

    public string ContentType { get; init; } = "";

    public string FileName { get; set; } = "";

    public string FullPath { get; set; } = "";
    
    public IReadOnlyList<object>? StorageFiles { get; init; } = null;
}