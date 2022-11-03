namespace DDS.Controls;

public class ReactiveViewLocator : IViewLocator
{
    public ReactiveViewLocator() // DI is working here, ReactiveViewLocator is Singleton
    {
    }
    
    public static Dictionary<Type, Type> DictOfViews { get; } = new(); 

    public IViewFor ResolveView<T>(T? viewModel, string? contract = null) =>
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        Globals.ServiceProvider.GetService(DictOfViews[viewModel?.GetType() ?? throw new NullReferenceException()]) 
            as IViewFor ?? throw new NullReferenceException();
}