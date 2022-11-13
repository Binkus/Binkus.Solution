using Avalonia.Platform.Storage;

namespace DDS.Avalonia.Services;

public class AvaloniaEssentialsCommonService : IAvaloniaEssentials
{
    private readonly Lazy<TopLevel> _topLevel;

    public AvaloniaEssentialsCommonService(Lazy<TopLevel> topLevel)
    {
        _topLevel = topLevel;
    }
    
    public async Task<Models.FilePickerResult> FilePickerAsync(bool allowMultiple = false)
    {
        if (Globals.IsDesignMode) return default;

        var results = await _topLevel.Value.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = allowMultiple
            });

        if (results?.Count > 0)
        {
            var result = results[0];

            var fileName = result.Name;
            
            if (result.TryGetUri(out var uri))
            {
                var fullPath = uri.AbsolutePath;
                
                return new Models.FilePickerResult
                {
                    ContentType = "",
                    FileName = fileName,
                    FullPath = fullPath,
                    StorageFiles = results
                };  
            }
        }

        return default;
    }
}