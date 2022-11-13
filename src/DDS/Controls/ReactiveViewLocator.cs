namespace DDS.Controls;

public class ReactiveViewLocator : IViewLocator
{
    private readonly IServiceProvider _services;

    public ReactiveViewLocator(IServiceProvider services) // DI is working here, ReactiveViewLocator is Singleton
    {
        _services = services;
    }
    
    public static Dictionary<string, Type> DictOfViews { get; } = new(); 

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null) =>
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        // BTW ViewModel property of the returned IViewFor will be set automatically to the same ViewModel as parameter
        // "T? viewModel" by ViewModelViewHost and RoutedViewHost, which are the ones who can call this method
        // the latter RoutedViewHost calls this method
        Globals.Services.GetService(DictOfViews[viewModel!.GetType().UnderlyingSystemType.FullName!]) 
            as IViewFor;

    // public IViewFor? ResolveByReflection<T>(T? viewModel, string? contract = null) // ResolveByReflection
    // {
    //     // Find view's by chopping of the 'Model' on the view model name
    //     // MyApp.ShellViewModel => MyApp.ShellView
    //     var viewModelName = viewModel?.GetType().FullName;
    //     var viewTypeName = viewModelName?.TrimEnd("Model".ToCharArray());
    //
    //     if (string.IsNullOrEmpty(viewTypeName))
    //     {
    //         return null;
    //     }
    //     
    //     try
    //     {
    //         var viewType = Type.GetType(viewTypeName);
    //         if (viewType == null)
    //         {
    //             // this.Log().Error($"Could not find the view {viewTypeName} for view model {viewModelName}.");
    //             return null;
    //         }
    //         return ActivatorUtilities.GetServiceOrCreateInstance(_services, viewType) as IViewFor;
    //     }
    //     catch (Exception)
    //     {
    //         // this.Log().Error($"Could not instantiate view {viewTypeName}.");
    //         throw;
    //     }
    // }
}

 