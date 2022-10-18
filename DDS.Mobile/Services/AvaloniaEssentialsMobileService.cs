using DDS.Services;
using Xamarin.Essentials;

namespace DDS.Mobile.Services;

public class AvaloniaEssentialsMobileService : IAvaloniaEssentials
{
    public async Task<Models.FileResult> FilePickerAsync()
    {
        FileResult fileResult = await FilePicker.PickAsync(new PickOptions { });
        
        return new Models.FileResult
        {
            ContentType = fileResult.ContentType,
            FileName = fileResult.FileName,
            FullPath = fileResult.FullPath,
            ReadStream = fileResult.OpenReadAsync
            
        };
    }
}