using System.Windows.Input;
using DDS.Core.Helper;
using DynamicData.Binding;

namespace DDS.Core.ViewModels;

public sealed class NavigationViewModel : NavigationViewModelBase<IViewModel>
{
    public NavigationViewModel() : this(Globals.Services) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public NavigationViewModel(IServiceProvider services) : base(services) { }
}

public sealed class NavigationViewModel<TForViewModel> : NavigationViewModelBase<TForViewModel>
    where TForViewModel : class, IViewModel
{
    public NavigationViewModel() : this(Globals.Services) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public NavigationViewModel(IServiceProvider services) : base(services) { }
}

public abstract partial class NavigationViewModelBase<TForViewModel> : ViewModelBase<TForViewModel>, INavigationViewModel<TForViewModel>
    where TForViewModel : class, IViewModel
{
    [IgnoreDataMember]
    public RoutingState Router { get; } = new();

    /// <inheritdoc cref="INavigationViewModel.BackCommand"/>
    [ObservableProperty, IgnoreDataMember]
    private ReactiveCommand<Unit, IRoutableViewModel?> _backCommand;

    /// <inheritdoc cref="INavigationViewModel.CanGoBack"/>
    [ObservableProperty, IgnoreDataMember]
    private IObservable<bool> _canGoBack;
    
    // [NotifyPropertyChangedFor(nameof(StackCount),nameof(CanGoBack), nameof(BackCommand), nameof(CanGoBackBool))]
    // [ObservableProperty, IgnoreDataMember] private int _backCountOffset;


    // private int _backCountOffset;
    // public int BackCountOffset
    // {
    //     get => _backCountOffset;
    //     set => this.RaiseAndSetIfChanged(ref _backCountOffset, value);
    // }

    [ObservableProperty] private int _backCountOffset;

    [ObservableProperty] private int _stackCount;

    [ObservableProperty] private bool _canGoBackBool;

    /// <summary>
    /// <p>Returns this.</p>
    /// <inheritdoc />
    /// </summary>
    [IgnoreDataMember]
    public sealed override IScreen HostScreen { get => base.HostScreen; protected init => base.HostScreen = value; }

    [ActivatorUtilitiesConstructor]
    protected NavigationViewModelBase(IServiceProvider services) : base(services)
    {
        HostScreen = this;

        this.WhenAnyValue(x => x.Router.NavigationStack.Count)
            .Subscribe(count =>
            {
                StackCount = Router.NavigationStack.Count - BackCountOffset;
                CanGoBackBool = StackCount > 0;
            });
        
        this.WhenAnyValue(x => x.BackCountOffset)
            .Subscribe(count =>
            {
                StackCount = Router.NavigationStack.Count - BackCountOffset;
                CanGoBackBool = StackCount > 0;
            });

        _canGoBack = CanGoBack = this.WhenPropertyChanged(_ => _.CanGoBackBool, true)
            .Select(x => x.Value);
        
        _backCommand = ReactiveCommand.CreateFromObservable(
            () => Router.NavigateBack.Execute(Unit.Default),
            CanGoBack);

        Console.WriteLine($"{UrlPathSegment}");
    }
    
    public void Reset() => Router.NavigationStack.Clear();
    
    public bool Back()
    {
        if (((ICommand)BackCommand).CanExecute(null) is false) return false;
        BackCommand.Execute(Unit.Default).SubscribeAndDisposeOnNext();
        return true;
    }
    
    // Safe generic navigation

    public bool To<TViewModel>(IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel
    {
        using var cmd = NavigateReactiveCommand<TViewModel>(canExecute);
        // if (cmd.CanExecute() is false) return false;
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).SubscribeAndDisposeOnNext();
        return true;
    }
    
    public bool ResetTo<TViewModel>(IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel
    {
        using var cmd = NavigateAndResetReactiveCommand<TViewModel>(canExecute);
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).SubscribeAndDisposeOnNext();
        return true;
    }
    
    // Navigation to runtime types
    
    public bool ResetTo(Type viewModelType, IObservable<bool>? canExecute = default)
    {
        using var cmd = NavigateAndResetReactiveCommand(viewModelType, canExecute);
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).SubscribeAndDisposeOnNext();
        return true;
    }
    
    public bool To(Type viewModelType, IObservable<bool>? canExecute = default)
    {
        using var cmd = NavigateReactiveCommand(viewModelType, canExecute);
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).SubscribeAndDisposeOnNext();
        return true;
    }
    
    //
    
    // // TODO cleanup after first commit of old tries
    
    // #nullable disable
    //
    // // partially working one
    // protected NavigationViewModelBase(IServiceProvider services, int shShow) : base(services)
    // {
    //     HostScreen = this;
    //     
    //     // CanGoBackBool = Router.NavigationStack.Count > 0;
    //     //
    //     // CanGoBackBool = true;
    //     //
    //     this.WhenAnyValue(x => x.Router.NavigationStack.Count)
    //         .Subscribe(count =>
    //         {
    //             StackCount = Router.NavigationStack.Count - BackCountOffset;
    //             CanGoBackBool = StackCount > 0;
    //         });
    //     
    //     this.WhenAnyValue(x => x.BackCountOffset)
    //         .Subscribe(count =>
    //         {
    //             StackCount = Router.NavigationStack.Count - BackCountOffset;
    //             CanGoBackBool = StackCount > 0;
    //         });
    //
    //     // _canGoBack = this.WhenAnyValue(_ => _.CanGoBackBool, (bool _) => _);
    //
    //     _canGoBack = CanGoBack = this.WhenPropertyChanged(_ => _.CanGoBackBool, true)
    //         .Select(x => x.Value);
    //     // CanGoBack.Subscribe();
    //     // CanGoBack.Subscribe(x =>
    //     // {
    //     //     Console.WriteLine(x);
    //     // });
    //
    //
    //     // var sub = this.WhenChanged(_ => _.CanGoBackBool, (_, b) => b).Subscribe();
    //     //
    //     // var o = Observable.Create((IObserver<bool> x) =>
    //     // {
    //     //     x.OnNext(CanGoBackBool);
    //     //     return sub;
    //     // });
    //     // _canGoBack = o;
    //     //
    //     _backCommand = ReactiveCommand.CreateFromObservable(
    //         () => Router.NavigateBack.Execute(Unit.Default),
    //         CanGoBack);
    //     // BackCommand.Subscribe();
    // }
    //
    // // borked
    // // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
    // public NavigationViewModelBase(IServiceProvider services, bool shShow) : base(services)
    // {
    //     HostScreen = this;
    //
    //     // this.WhenAnyValue(x => x.Router.NavigationStack.Count).Subscribe(count =>
    //     //     this.RaisePropertyChanged(new PropertyChangedEventArgs(nameof(NavigationStackCount))));
    //
    //     // this.WhenAnyValue(x => x.Router.NavigationStack.Count)
    //     //     .Subscribe(count => OnPropertyChanged(nameof(StackCount)));
    //     
    //     // this.WhenAnyValue(x => x.StackCount)
    //     //     .Subscribe(count =>
    //     //     {
    //     //         Console.WriteLine($"StackCount changed to {count}");
    //     //         OnPropertyChanging(nameof(BackCommand));
    //     //         OnPropertyChanged(nameof(BackCommand));
    //     //         CanGoBackBool = count > 0;
    //     //     });
    //
    //     // this
    //     //     .WhenAnyValue(x => x.StackCount).Subscribe(count =>
    //     //     {
    //     //         CanGoBack = this
    //     //             .WhenAnyValue(x => x.Router.NavigationStack.Count)
    //     //             .Select(c => c > BackCountOffset);
    //     //         
    //     //         BackCommand = ReactiveCommand.CreateFromObservable(
    //     //             () => Router.NavigateBack.Execute(Unit.Default),
    //     //             CanGoBack);
    //     //     });
    //     
    //     // _canGoBack = this
    //     //     .WhenAnyValue(x => x.Router.NavigationStack.Count)
    //     //     .Select(count => count > BackCountOffset);
    //     
    //     // _canGoBack = this.WhenAnyValue(
    //     //     x => x.Router.NavigationStack.Count,
    //     //     x => x.BackCountOffset,
    //     //     x => x.StackCount,
    //     //     (_, _, _) => StackCount > 0);
    //     //
    //     // _canGoBack.Subscribe(x => CanGoBackBool = x);
    //     
    //     CanGoBackBool = Router.NavigationStack.Count > 0;
    //     
    //     this
    //         .WhenAnyValue(x => x.Router.NavigationStack.Count)
    //         .Subscribe(count =>
    //         {
    //             StackCount = count - BackCountOffset;
    //         });
    //
    //     _canGoBack = this
    //         .WhenAnyValue(x => x.CanGoBackBool);
    //     
    //     
    //     // var o = this.WhenAnyValue(x => x.StackCount,
    //     //     stackCount => stackCount > 0                   
    //     // );
    //     
    //     // // doesn't work somehow
    //     // _canGoBack = this
    //     //     .WhenAnyValue(x => x.Router.NavigationStack.Count, 
    //     //         x => x.BackCountOffset)
    //     //     .Select(((int NavStackCountOfRouter, int BackCountOffset)x) => x.NavStackCountOfRouter > x.BackCountOffset);
    //     
    //     _backCommand = ReactiveCommand.CreateFromObservable(
    //         () => Router.NavigateBack.Execute(Unit.Default),
    //         CanGoBack);
    // }
    // #nullable restore
}