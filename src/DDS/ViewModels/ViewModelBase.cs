// ReSharper disable MemberCanBePrivate.Global

namespace DDS.ViewModels;

[DataContract]
public abstract class ViewModelBase : ObservableObject,
    IReactiveNotifyPropertyChanged<IReactiveObject>, IRoutableViewModel, IActivatableViewModel, IProvideServices
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

    protected ViewModelBase(IScreen hostScreen) : this() => _lazyHostScreen = new Lazy<IScreen>(hostScreen);
    protected ViewModelBase(Lazy<IScreen> lazyHostScreen) : this() => _lazyHostScreen = lazyHostScreen;
    protected ViewModelBase(IServiceProvider services) : this() => Services = services;
    
    protected ViewModelBase()
    {
        ReactiveObjectCompatibility();
        
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
    
    [IgnoreDataMember] public IServiceProvider Services { get; protected init; } = Globals.Services;

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

    //

    #region Compatibility for ReactiveUI, IReactiveObject x ObservableObject, CommunityToolkit.Mvvm

    /// <summary>
    /// Call this once from ctor of this base class for quick setup for ReactiveUI compatibility,
    /// without SetupReactiveObject() which is called by this method, ReactiveUI would not be able to notify our
    /// INotifyProperty*-implementations, so e.g. ReactiveUI.Fody would not work without it 
    /// </summary>
    private void ReactiveObjectCompatibility()
    {
        SetupReactiveNotifyPropertyChanged();
        SetupReactiveObject();
    }

    /// <summary>
    /// Important setup for this ViewModel that ReactiveUI is able to notify our INotifyProperty*-implementations
    /// </summary>
    private void SetupReactiveObject()
    {
        this.SubscribePropertyChangingEvents();
        this.SubscribePropertyChangedEvents();
    }
    
    // IReactiveObject (inherited from IRoutableViewModel)

    public virtual void RaisePropertyChanging(PropertyChangingEventArgs args) => base.OnPropertyChanging(args);

    public virtual void RaisePropertyChanged(PropertyChangedEventArgs args) => base.OnPropertyChanged(args);
    
    // IReactiveNotifyPropertyChanged<IReactiveObject>
    
    [IgnoreDataMember] private Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>> _changing = null!;
    [IgnoreDataMember] private Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>> _changed = null!;

    /// <summary>
    /// Sets up Observables for IReactiveNotifyPropertyChanged
    /// </summary>
    private void SetupReactiveNotifyPropertyChanged()
    {
        _changing = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(
            () => Observable.FromEventPattern<PropertyChangingEventHandler, PropertyChangingEventArgs>
            (
                changingHandler => PropertyChanging += changingHandler,
                changingHandler => PropertyChanging -= changingHandler
            ).Select(eventPattern => // new ReactivePropertyChangedEventArgs works too, interface uses IReactivePropertyChangedEventArgs
                new ReactivePropertyChangingEventArgs<ViewModelBase>(
                    (eventPattern.Sender as ViewModelBase)!, eventPattern.EventArgs.PropertyName!)));

        _changed = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(
            () => Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>
            (
                changedHandler => PropertyChanged += changedHandler,
                changedHandler => PropertyChanged -= changedHandler
            ).Select(eventPattern => 
                new ReactivePropertyChangedEventArgs<ViewModelBase>(
                    (eventPattern.Sender as ViewModelBase)!, eventPattern.EventArgs.PropertyName!)));
    }

    /// <summary>
    /// Implementation of ReactiveObject calls ReactiveUI-internal functions, this one needs testing and may not work.
    /// <inheritdoc />
    /// </summary>
    /// <returns><inheritdoc /></returns>
    public virtual IDisposable SuppressChangeNotifications() => Disposable.Empty;
    
    [IgnoreDataMember]
    public virtual IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing
    {
        get => _changing.Value;
        set => _changing = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(value);
    }

    [IgnoreDataMember]
    public virtual IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed
    {
        get => _changed.Value;
        set => _changed = new Lazy<IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>>>(value);
    }

    #endregion
}
