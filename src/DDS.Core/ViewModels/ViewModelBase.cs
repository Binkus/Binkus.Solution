// ReSharper disable MemberCanBePrivate.Global

using DDS.Core.Services;

namespace DDS.Core.ViewModels;

[DataContract]
public abstract class ViewModelBase : ReactiveObservableObject,
    IRoutableViewModel, IActivatableViewModel, IProvideServices
{
    [IgnoreDataMember] public string? UrlPathSegment { get; }

    [IgnoreDataMember] private Lazy<IScreen>? _lazyHostScreen;
    
    [IgnoreDataMember, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IScreen HostScreen
    {
        get => _lazyHostScreen?.Value ?? this.RaiseAndSetIfChanged(
            ref _lazyHostScreen, new Lazy<IScreen>(GetService<IScreen>()))!.Value;
        protected init => this.RaiseAndSetIfChanged(ref _lazyHostScreen, new Lazy<IScreen>(value));
    }
    
    [IgnoreDataMember, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public NavigationViewModel Navigation => HostScreen as NavigationViewModel ?? GetService<NavigationViewModel>();

    [IgnoreDataMember] public virtual RoutingState Router => HostScreen.Router;

    [IgnoreDataMember] public ViewModelActivator Activator { get; } = new();
    
    [IgnoreDataMember] public string ViewModelName { get; private init; }
    [IgnoreDataMember] private string? _customViewName;

    [DataMember]
    public string CustomViewName
    {
        get => _customViewName ??= ViewModelName.EndsWith("ViewModel") ? ViewModelName[..^9] : ViewModelName;
        set => this.RaiseAndSetIfChanged(ref _customViewName, value);
    }

    protected ViewModelBase(IServiceProvider services, IScreen hostScreen) : this(services) => _lazyHostScreen = new Lazy<IScreen>(hostScreen);
    protected ViewModelBase(IServiceProvider services, Lazy<IScreen> lazyHostScreen) : this(services) => _lazyHostScreen = lazyHostScreen;
    // protected ViewModelBase(IServiceProvider services) : this() => Services = services;
    
    protected ViewModelBase(IServiceProvider services)
    {
        Services = services;
        ViewModelName = GetType().UnderlyingSystemType.Name;
        UrlPathSegment = $"/{CustomViewName.ToLowerInvariant()}";

        this.WhenActivated(disposables => 
        {
            Debug.WriteLine(UrlPathSegment + ":");

            HandleActivation();
            Disposable
                .Create(HandleDeactivation)
                .DisposeWith(disposables);
        });
        
        Debug.WriteLine("c:"+ViewModelName);
    }
    
    protected virtual void HandleActivation() { }
    protected virtual void HandleDeactivation() { }
    
    [IgnoreDataMember] public IServiceProvider Services { get; protected init; } //= Globals.Services;

    public TService GetService<TService>() where TService : notnull => Services.GetRequiredService<TService>();
    
    public object GetService(Type serviceType) => Services.GetRequiredService(serviceType);

    
    //

    protected ReactiveCommand<Unit, IRoutableViewModel> NavigateReactiveCommand<TViewModel>(
        ) where TViewModel : class, IRoutableViewModel 
        => CreateNavigationReactiveCommandFromObservable<TViewModel>(new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => Router.Navigate));
    
    protected ReactiveCommand<Unit, IRoutableViewModel> NavigateAndResetReactiveCommand<TViewModel>(
        ) where TViewModel : class, IRoutableViewModel 
        => CreateNavigationReactiveCommandFromObservable<TViewModel>(new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => Router.NavigateAndReset));

    protected ReactiveCommand<Unit, IRoutableViewModel> CreateNavigationReactiveCommandFromObservable<TViewModel>(
        Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>> navi) where TViewModel : class, IRoutableViewModel 
        => ReactiveCommand.CreateFromObservable(
            () => navi.Value.Execute(GetService<TViewModel>()),
            canExecute: this.WhenAnyObservable(x => x.Router.CurrentViewModel).Select(x => x is not TViewModel)
        );
}
