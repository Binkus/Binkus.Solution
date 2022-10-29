using System.Runtime.Versioning;
using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Web.Blazor;

namespace DDS.Web;

public partial class App
{
    // protected override void OnParametersSet()
    // {
    //     base.OnParametersSet();
    //
    //     WebAppBuilder.Configure<DDS.App>()
    //         // .UseReactiveUI()
    //         .ConfigureAppServices(services =>
    //         {
    //             
    //         })
    //         .SetupWithSingleViewLifetime();
    // }
    
    [SupportedOSPlatform("browser")]
    protected override void OnParametersSet()
    {
        AppBuilder.Configure<DDS.App>()
            .UseBlazor()
            .ConfigureAppServices(services =>
            {
                
            })
            // .With(new SkiaOptions { CustomGpuFactory = null }) // uncomment to disable GPU/GL rendering
            .SetupWithSingleViewLifetime();

        base.OnParametersSet();
    }
}