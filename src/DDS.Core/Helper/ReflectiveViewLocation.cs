namespace DDS.Core.Helper;

public static class ReflectiveViewLocation
{
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

    public static Type? GetViewType(Type viewModelType)
    {
        var viewName = viewModelType.Name.Replace("ViewModel", "View");
        
        foreach (var type in SearchNamespaces.Select(searchName => searchName + viewName).Select(Type.GetType).Where(type => type is not null))
            return type;

        viewName = viewModelType.Name.Replace("View", "Window");
        
        foreach (var type in SearchNamespaces.Select(searchName => searchName + viewName).Select(Type.GetType).Where(type => type is not null))
            return type;

        foreach (var type in SearchNamespaces.Select(searchName => searchName.Replace("View", "Window") + viewName).Select(Type.GetType).Where(type => type is not null))
            return type;

        viewName = viewModelType.Name.Replace("Window", "");
        
        foreach (var type in SearchNamespaces.Select(searchName => searchName + viewName).Select(Type.GetType).Where(type => type is not null))
            return type;

        foreach (var type in SearchNamespaces.Select(searchName => searchName.Replace("View", "Window") + viewName).Select(Type.GetType).Where(type => type is not null))
            return type;

        foreach (var type in SearchNamespaces.Select(searchName => searchName.Replace("Views.", "") + viewName).Select(Type.GetType).Where(type => type is not null))
            return type;

        return SearchNamespaces.Select(searchName => searchName.Replace("Views", "Controls") + viewName).Select(Type.GetType).FirstOrDefault(type => type is not null);
    }
}