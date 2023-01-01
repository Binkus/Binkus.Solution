using Binkus.DependencyInjection;

namespace Binkus.ReactiveMvvm;

public interface IViewModel 
    : IRoutableViewModel, IActivatableViewModel, IProvideServices, IReactiveNotifyPropertyChanged<IReactiveObject>,
        IInitializable
{
    INavigationViewModel Navigation { get; }
    string ViewModelName => GetType().Name;
    string CustomViewName => RawViewName;
    string RawViewName => TryGetRawViewName(ViewModelName);
    string IRoutableViewModel.UrlPathSegment => RawViewName.ToLowerInvariant();
    
    // removes "ViewModel" or e.g. "ViewModel'1" at the end if possible, "MainViewModel" => "Main"
    public static string TryGetRawViewName(string viewModelName) =>
        viewModelName.EndsWith("ViewModel") ? viewModelName[..^9]
        : viewModelName[^11..^2] == "ViewModel" ? viewModelName[..^11] : viewModelName;
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