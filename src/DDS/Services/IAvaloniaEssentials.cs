namespace DDS.Services;

public interface IAvaloniaEssentials
{
    Task<Models.FileResult> FilePickerAsync(bool allowMultiple = false);
}