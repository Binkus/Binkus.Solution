using Avalonia.ReactiveUI;
using Avalonia.Web.Blazor;

namespace DDS.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        WebAppBuilder.Configure<DDS.App>()
            .UseReactiveUI()
            .SetupWithSingleViewLifetime();
    }
}