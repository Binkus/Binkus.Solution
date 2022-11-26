using DDS.Core.Models;

namespace DDS.Core.Services;

public interface IAvaloniaEssentials
{
    Task<FilePickerResult> FilePickerAsync(bool allowMultiple = false);
}