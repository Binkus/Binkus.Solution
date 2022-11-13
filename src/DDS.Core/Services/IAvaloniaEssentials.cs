namespace DDS.Avalonia.Services;

public interface IAvaloniaEssentials
{
    Task<Models.FilePickerResult> FilePickerAsync(bool allowMultiple = false);
}