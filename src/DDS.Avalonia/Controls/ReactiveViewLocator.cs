using DDS.Core;

// ReSharper disable HeapView.PossibleBoxingAllocation

namespace DDS.Avalonia.Controls;

public class ReactiveViewLocator : IViewLocator
{
    public ReactiveViewLocator() // DI is working here, ReactiveViewLocator is Singleton
    {
    }
    
    public ReactiveViewLocator(Dictionary<string, Type> dictOfViews) => DictOfViews = dictOfViews;
    
    private Dictionary<string, Type> DictOfViews { get; } = Globals.ViewModelNameViewTypeDictionary;

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null) =>
        // BTW ViewModel property of the returned IViewFor will be set automatically to the same ViewModel as parameter
        // "T? viewModel" by ViewModelViewHost and RoutedViewHost, which are the ones who can call this method
        // the latter RoutedViewHost calls this method
        (viewModel is IViewModel vm ? vm.Services : Globals.Services)
        .GetService(DictOfViews[(viewModel as IViewModel)?.FullNameOfType 
                                ?? (viewModel is null ? throw new NullReferenceException() 
                                    : viewModel)
                                .GetType().UnderlyingSystemType.FullName ?? throw new UnreachableException()
                                ]) as IViewFor;
}