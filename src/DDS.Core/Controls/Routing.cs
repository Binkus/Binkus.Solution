using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace DDS.Core.Controls;

public sealed class Routing
{
    private Dictionary<string, Type> RoutesMut { get; } = new();
    
    public ReadOnlyDictionary<string, Type> Routes { get; }

    public Routing() => Routes = new ReadOnlyDictionary<string, Type>(RoutesMut);


    public bool RegisterRoute<TViewModel>(string route) => RegisterRoute(route, typeof(TViewModel));

    public bool RegisterRoute(string route, Type viewModelType) =>
        !IsReadOnly && RoutesMut.TryAdd(route, viewModelType);

    public bool IsReadOnly { get; private set; }

    public void MakeReadonly() => IsReadOnly = true;
}