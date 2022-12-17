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

        type ??= GetViewType(data.GetType());

        return services.TryGetServiceOrCreateInstance<IControl>(type);
    }
    
    static ViewLocator()
    {
        AddViewSearchPath(typeof(MainView));
    }

    public static void AddViewSearchPath(Type viewType)
    {
        var name = viewType.Name.Replace("ViewModel", "View");
        var qualifiedName = viewType.FullName?.Replace("ViewModel", "View")
                ?? throw new UnreachableException();
        qualifiedName = qualifiedName.Remove(qualifiedName.Length - name.Length, name.Length);
        SearchNamespaces.Add(qualifiedName);
    }
    
    public static void AddViewSearchPathAssemblyRootNamespace(Type assemblyMarkerType, bool appendViewsSubdirectory = true)
    {
        var name = assemblyMarkerType.Name;
        var qualifiedName = assemblyMarkerType.FullName ?? throw new UnreachableException();
        qualifiedName = qualifiedName.Remove(qualifiedName.Length - name.Length, name.Length);
        SearchNamespaces.Add(appendViewsSubdirectory ? qualifiedName + "Views." : qualifiedName);
    }

    private static readonly List<string> SearchNamespaces = new(2);

    // todo evaluate moving to Core
    // ReSharper disable once CognitiveComplexity
    public static Type? GetViewType(Type viewModelType)
    {
        // var vmFullName = viewModelType.FullName ?? throw new UnreachableException();
        var viewName = viewModelType.Name.Replace("ViewModel", "View");
        
        // static Type? Fun(string searchName, string viewName)
        // {
        //     var name = searchName + viewName;
        //     
        //     var type = Type.GetType(name);
        //
        //     return type;
        // }
        //
        // static bool Fun2(string searchName, string viewName, out Type? type)
        // {
        //     var name = searchName + viewName;
        //     
        //     type = Type.GetType(name);
        //     
        //     return type is not null;
        // }
        
        // todo single loop to reduce complexity :)

        foreach (string searchName in SearchNamespaces)
        {
            var name = searchName + viewName;
            
            var type = Type.GetType(name);

            if (type is not null) return type;
        }
        
        viewName = viewModelType.Name.Replace("View", "Window");
        
        foreach (string searchName in SearchNamespaces)
        {
            var name = searchName + viewName;
            
            var type = Type.GetType(name);

            if (type is not null) return type;
        }
        
        foreach (string searchName in SearchNamespaces)
        {
            var name = searchName.Replace("View", "Window") + viewName;
            
            var type = Type.GetType(name);

            if (type is not null) return type;
        }
        
        viewName = viewModelType.Name.Replace("Window", "");
        
        foreach (string searchName in SearchNamespaces)
        {
            var name = searchName + viewName;
            
            var type = Type.GetType(name);

            if (type is not null) return type;
        }
        
        foreach (string searchName in SearchNamespaces)
        {
            var name = searchName.Replace("View", "Window") + viewName;
            
            var type = Type.GetType(name);

            if (type is not null) return type;
        }
        
        foreach (string searchName in SearchNamespaces)
        {
            var name = searchName.Replace("Views.", "") + viewName;
            
            var type = Type.GetType(name);

            if (type is not null) return type;
        }
        
        foreach (string searchName in SearchNamespaces)
        {
            var name = searchName.Replace("Views", "Controls") + viewName;
            
            var type = Type.GetType(name);

            if (type is not null) return type;
        }
        
        return null;
    }
}