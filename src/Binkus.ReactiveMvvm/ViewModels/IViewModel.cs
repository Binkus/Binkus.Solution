using Binkus.DependencyInjection;

namespace Binkus.ReactiveMvvm;

public interface IViewModel 
    : IRoutableViewModel, IActivatableViewModel, IProvideServices, IReactiveNotifyPropertyChanged<IReactiveObject>,
        IInitializable
{
    INavigationViewModel Navigation { get; }
    string ViewModelName { get; }
    string RawViewName { get; }
    string CustomViewName { get; set; }
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