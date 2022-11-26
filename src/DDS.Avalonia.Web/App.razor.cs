using System.Runtime.Versioning;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Web.Blazor;

namespace DDS.Avalonia.Web;

public partial class App
{
    [SupportedOSPlatform("browser")]
    protected override void OnParametersSet()
    {
        AppBuilder.Configure<DDS.Avalonia.App>()
            .UseBlazor()
            .ConfigureAppServices(services =>
            {
                
            })
            // .With(new SkiaOptions { CustomGpuFactory = null }) // uncomment to disable GPU/GL rendering
            .SetupWithSingleViewLifetime();

        base.OnParametersSet();
    }
}