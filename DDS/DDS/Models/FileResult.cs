namespace DDS.Models;

public class FileResult
{
    public string? ContentType { get; init; }

    public string? FileName { get; set; }
    
    public string? FullPath { get; set; }

    public Func<Task<Stream>>? ReadStream { get; set; }
}