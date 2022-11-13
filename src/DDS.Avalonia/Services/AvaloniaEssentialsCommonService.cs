using Avalonia.Platform.Storage;
using DDS.Core;
using DDS.Core.Models;
using DDS.Core.Services;

namespace DDS.Avalonia.Services;

public class AvaloniaEssentialsCommonService : IAvaloniaEssentials
{
    // private readonly Lazy<Task<TopLevel>> _topLevel;
    //
    // public AvaloniaEssentialsCommonService(Lazy<TopLevelService> tls)
    // {
    //     _topLevel = new Lazy<Task<TopLevel>>(() => tls.Value.CurrentTopLevel());
    // }
    
    private readonly TopLevelService _topLevelService;

    public AvaloniaEssentialsCommonService(TopLevelService topLevelService)
    {
        _topLevelService = topLevelService;
    }
    
    public async Task<FilePickerResult> FilePickerAsync(bool allowMultiple = false)
    {
        if (Globals.IsDesignMode) return default;

        var topLevel = await _topLevelService.CurrentTopLevel();
        
        // var results = await (await _topLevel.Value).StorageProvider.OpenFilePickerAsync(
        var results = await topLevel.StorageProvider.OpenFilePickerAsync(
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

                return new FilePickerResult
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