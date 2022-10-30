using System.Reflection;
using Avalonia.Platform.Storage;

namespace DDS.Services;

public class AvaloniaEssentialsCommonService : IAvaloniaEssentials
{
    private readonly Lazy<TopLevel> _topLevel;


    public AvaloniaEssentialsCommonService()
    {
        _topLevel = null!;
    }
    
    [ActivatorUtilitiesConstructor]
    public AvaloniaEssentialsCommonService(Lazy<TopLevel> topLevel)
    {
        _topLevel = topLevel;
    }
    
    public async Task<Models.FileResult> FilePickerAsync(bool allowMultiple = false)
    {
        var results = await _topLevel.Value.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
                
                return new Models.FileResult
                {
                    ContentType = "",
                    FileName = fileName,
                    FullPath = fullPath,
                    StorageFiles = results
                };  
            }
        }
        
        return new Models.FileResult
        {
            ContentType = "",
            FileName = "",
            FullPath = "",
            StorageFiles = null
        };
    }
}