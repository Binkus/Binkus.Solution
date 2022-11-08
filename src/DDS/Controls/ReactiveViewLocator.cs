namespace DDS.Controls;

public class ReactiveViewLocator : IViewLocator
{
    public ReactiveViewLocator() // DI is working here, ReactiveViewLocator is Singleton
    {
    }
    
    public static Dictionary<string, Type> DictOfViews { get; } = new(); 

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null) =>
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        // BTW ViewModel property of the returned IViewFor will be set automatically to the same ViewModel as parameter
        // "T? viewModel" by ViewModelViewHost and RoutedViewHost, which are the ones who can call this method
        // the latter RoutedViewHost calls this method
        Globals.ServiceProvider.GetService(DictOfViews[viewModel!.GetType().UnderlyingSystemType.FullName!]) 
            as IViewFor;
}