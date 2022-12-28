using Binkus.ReactiveMvvm;

namespace DDS.Core.ViewModels;

public interface IViewModel 
    : IRoutableViewModel, IActivatableViewModel, IProvideServices, IReactiveNotifyPropertyChanged<IReactiveObject>,
        IInitializable
{
    Guid InstanceId { get; }
    INavigationViewModel Navigation { get; }
    string ViewModelName { get; }
    string RawViewName { get; }
    string CustomViewName { get; set; }
    
    //

    ReactiveCommand<Unit, IRoutableViewModel> NavigateReactiveCommand<TViewModel>(IObservable<bool>? canExecute = default
    ) where TViewModel : class, IRoutableViewModel;

    ReactiveCommand<Unit, IRoutableViewModel> NavigateAndResetReactiveCommand<TViewModel>(IObservable<bool>? canExecute = default
    ) where TViewModel : class, IRoutableViewModel;

    ReactiveCommand<Unit, IRoutableViewModel> CreateNavigationReactiveCommandFromObservable<TViewModel>(
        Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>> navi, IObservable<bool>? canExecute = default)
        where TViewModel : class, IRoutableViewModel;
    
    //
    
    ReactiveCommand<Unit, IRoutableViewModel> NavigateReactiveCommand(Type viewModelType, IObservable<bool>? canExecute = default);
    
    ReactiveCommand<Unit, IRoutableViewModel> NavigateAndResetReactiveCommand(Type viewModelType, IObservable<bool>? canExecute = default);

    ReactiveCommand<Unit, IRoutableViewModel> CreateNavigationReactiveCommandFromObservable(Type viewModelType,
        Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>> navi, IObservable<bool>? canExecute = default);
}

public interface IViewModelBase<T> : IViewModel
    where T : class, IViewModel
{
    
}

public interface IViewModelBase : IViewModel { }


public interface IInitializable
{
    protected void Initialize(CancellationToken cancellationToken);
    public sealed void InitializeOnceAfterCreation(CancellationToken cancellationToken) => Initialize(cancellationToken);
}