namespace DDS.Services;

public interface IAvaloniaEssentials
{
    Task<Models.FileResult> FilePickerAsync();
}