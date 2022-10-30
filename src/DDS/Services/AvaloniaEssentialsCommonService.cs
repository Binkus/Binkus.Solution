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
        // fileResult = await FilePicker.PickAsync(new PickOptions { });
        
        // var result = await new OpenFileDialog()
        // {
        //     Title = "Open file",
        //     // Filters = GetFilters(),
        //     // Directory = lastSelectedDirectory,
        //     // Almost guaranteed to exist
        //     InitialFileName = Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName
        // }.ShowAsync(_window);

        // var win = Globals.ServiceProvider.GetRequiredService<MainWindow>();

        // var results = await win.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
                    ReadStream = null!
            
                };  
            }
            
                      
        } 
        
        
        // dynamic fileResult = null!;
        
        return new Models.FileResult
        {
            ContentType = "",
            FileName = "",
            FullPath = "",
            ReadStream = null!
            
        };
    }
}