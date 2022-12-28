using DDS.Core.Helper;
using DDS.Core.ViewModels;

// ReSharper disable HeapView.PossibleBoxingAllocation

namespace DDS.Core.Controls;

public sealed class ReactiveViewLocator : IViewLocator
{
    // bool DoesTryGetDefault
    
    private readonly bool _doReflectiveSearch;
    private readonly bool _triesGettingDefault;

    [ActivatorUtilitiesConstructor] // DI is working here, ReactiveViewLocator is a Singleton by default
    public ReactiveViewLocator(bool doReflectiveSearch = false, bool triesGettingDefault = false) : this(null, doReflectiveSearch, triesGettingDefault) { }
    
    [UsedImplicitly]
    public ReactiveViewLocator(Dictionary<string, Type>? dictOfViews, bool doReflectiveSearch = false, bool triesGettingDefault = false)
    {
        DictOfViews = dictOfViews ?? Globals.ViewModelNameViewTypeDictionary;
        _doReflectiveSearch = doReflectiveSearch;
        _triesGettingDefault = triesGettingDefault;
    }
    
    private Dictionary<string, Type> DictOfViews { get; }

    /// <summary>
    /// <inheritdoc cref="IViewLocator.ResolveView{T}"/>
    /// When viewModel is null, resolves IViewFor from Global IServiceProvider.
    /// When viewModel not null, gets view type to resolve from Dictionary if set. When viewModel is not null and
    /// when result from previous search is null tries to get view service by searching for
    /// IViewFor&lt;typeof(viewModel)&gt;. If still null tries to find registered interface
    /// with similar name and tries getting the service through that, if still null, tries more reflective search if
    /// enabled during ReactiveViewLocator creation.
    /// <p>The ViewModel property (DataContext) of the returned IViewFor will be set automatically
    /// to the same ViewModel as the parameter
    /// "T? viewModel" by ViewModelViewHost and RoutedViewHost, which are the ones who can call this method
    /// the latter RoutedViewHost calls this method in this template.</p>
    /// </summary>
    /// <param name="viewModel">ViewModel, should not be null by normal usage of e.g. RoutedViewHost</param>
    /// <param name="contract">Contract from Splat, ignored.</param>
    /// <typeparam name="T"><inheritdoc cref="IViewLocator.ResolveView{T}"/></typeparam>
    /// <returns><inheritdoc cref="IViewLocator.ResolveView{T}"/></returns>
    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null) =>
        viewModel is null ? TryGetDefault(null, _triesGettingDefault) // tries to get global default IViewFor when VM null
            : (viewModel is IViewModel vm ? vm.Services : Globals.Services)
            .GetService(DictOfViews.TryGetValue(viewModel.GetType().UnderlyingSystemType.FullName
                                                ?? throw new UnreachableException(), out var type)
                ? type : typeof(IViewFor<>).MakeGenericType(viewModel.GetType())
                ) as IViewFor ?? TryGetViewByInterfaceWithSameName(viewModel, DictOfViews)
                              ?? (_doReflectiveSearch ? TryGetViewByReflectionSearch(viewModel) : null)
                              ?? TryGetDefault(viewModel, _triesGettingDefault);


    private static IViewFor? TryGetDefault(object? viewModel, bool triesGettingDefault = true)
    {
        if (!triesGettingDefault) return null;
        var vm = viewModel as IViewModel;
        var services = vm?.Services ?? Globals.Services;
        return services.GetService<IViewFor>();
    }
    
    // private static Type? TryGetViewTypeByInterfaceWithSameName(object viewModel)
    // {
    //     var vmType = viewModel.GetType().UnderlyingSystemType;
    //     var ivmType = vmType.GetInterface('I' + vmType.Name);
    //     return ivmType;
    // }
    
    private static IViewFor? TryGetViewByInterfaceWithSameName(object viewModel, IReadOnlyDictionary<string, Type>? dictOfViews = null)
    {
        var vm = viewModel as IViewModel;
        var services = vm?.Services ?? Globals.Services;
        var vmType = viewModel.GetType().UnderlyingSystemType;
        var ivmType = vmType.GetInterface('I' + vmType.Name);
        if (ivmType is null) return null;
        // var viewForType = typeof(IViewFor<>).MakeGenericType(ivmType);
        return dictOfViews?.TryGetValue(ivmType.FullName ?? throw new UnreachableException(), out var viewType) ?? false
            ? services.GetService(viewType) as IViewFor ?? services.GetService(typeof(IViewFor<>).MakeGenericType(ivmType)) as IViewFor
            : services.GetService(typeof(IViewFor<>).MakeGenericType(ivmType)) as IViewFor;
    }
    
    private static IViewFor? TryGetViewByReflectionSearch(object viewModel)
    {
        var vm = viewModel as IViewModel;
        var services = vm?.Services ?? Globals.Services;
        var vmType = viewModel.GetType().UnderlyingSystemType;
        var vType = ReflectiveViewLocation.GetViewType(vmType);
        // todo check with VM interface

        return TryGetOrCreateView(services, vType);
    }

    private static IViewFor? TryGetOrCreateView(IServiceProvider services, 
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type? vType)
        => services.TryGetServiceOrCreateInstance<IViewFor>(vType);
}