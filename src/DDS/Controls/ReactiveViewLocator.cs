using DDS.Services;

namespace DDS.Controls;

public class ReactiveViewLocator : ReactiveUI.IViewLocator
{
    public ReactiveViewLocator() // DI is working here, ReactiveViewLocator is Singleton
    {
    }
    
    public static Dictionary<Type, Type> DictOfViews { get; } = new(); 

    public IViewFor ResolveView<T>(T? viewModel, string? contract = null) =>
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        Globals.ServiceProvider.GetService(DictOfViews[viewModel?.GetType() ?? throw new NullReferenceException()]) 
            as IViewFor ?? throw new NullReferenceException();
    
    // public IViewFor? ResolveView<T>(T? viewModel, string? contract) 
    //     => ResolveView(viewModel as ViewModelBase ?? throw new NullReferenceException());
    //
    // private IViewFor ResolveView<T>(T viewModel) where T : ViewModelBase
    // {
    //     var view = Globals.ServiceProvider.GetService(DictOfViews[viewModel.GetType()]) as IViewFor 
    //                ?? throw new NullReferenceException();
    //     view.ViewModel = viewModel;
    //     return view;
    // }


    // private TestView? _tv;
    // private SecondTestView? _stv;
    //
    // public IViewFor? ResolveView2<T>(T? viewModel, string? contract = null) => viewModel switch
    // {
    //     TestViewModel context => _tv ??= new TestView() { DataContext = context },
    //     SecondTestViewModel context => _stv ??= new SecondTestView() { DataContext = context },
    //     _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
    // };
    //
    //
    // public IViewFor? ResolveView22<T>(T? viewModel, string? contract = null) => viewModel switch
    // {
    //     TestViewModel context => new TestView() { DataContext = context },
    //     SecondTestViewModel context => new SecondTestView() { DataContext = context },
    //     _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
    // };

    // public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
    // {
    //     throw new NotImplementedException();
    // }
}