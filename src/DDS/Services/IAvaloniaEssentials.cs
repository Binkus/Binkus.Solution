namespace DDS.Services;

public interface IAvaloniaEssentials
{
    Task<Models.FilePickerResult> FilePickerAsync(bool allowMultiple = false);
}