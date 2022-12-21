using System.Reflection;
using Avalonia.Controls.Templates;
using DDS.Core;
using DDS.Core.Helper;

namespace DDS.Avalonia.Controls;

public sealed class ViewLocator : IDataTemplate
{
    private IViewLocator? _reactiveViewLocator;

    [UsedImplicitly] public ViewLocator(/* NO DI here, called by App.axaml */) {/*Global ServiceProvider not yet set*/}
    

    // When firstly called by framework the Global ServiceProvider (Global.Services) is initialized already.
    public IControl? Build(object? data) =>
        (_reactiveViewLocator ??= Globals.GetService<IViewLocator>())
        .ResolveView(data) as IControl ?? BackupBuild(data);

    public bool Match(object? data) => data is IViewModel;
    
    private static IControl? BackupBuild(object? data)
    {
        if (data is null) return null;
        
        var vm = data as IViewModel;
        var services = vm?.Services ?? Globals.Services;
        
        var name = data.GetType().FullName?.Replace("ViewModel", "View") 
                   ?? throw new UnreachableException();
        var type = Type.GetType(name);

        type ??= ReflectiveViewLocation.GetViewType(data.GetType());

        return services.TryGetServiceOrCreateInstance<IControl>(type);
    }
    
    static ViewLocator()
    {
        ReflectiveViewLocation.AddViewSearchPath(typeof(MainView));
    }
}