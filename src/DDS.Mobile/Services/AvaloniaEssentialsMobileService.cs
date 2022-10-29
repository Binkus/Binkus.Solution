using DDS.Services;
using Microsoft.Maui.Storage;

namespace DDS.Mobile.Services;

public class AvaloniaEssentialsMobileService : IAvaloniaEssentials
{
    public async Task<Models.FileResult> FilePickerAsync(bool allowMultiple = false)
    {
        FileResult? fileResult = await FilePicker.PickAsync(new PickOptions { });

        return fileResult is null 
            ? default
            : new Models.FileResult 
            {
                ContentType = fileResult.ContentType,
                FileName = fileResult.FileName,
                FullPath = fileResult.FullPath,
                ReadStream = fileResult.OpenReadAsync
            };
    }
}