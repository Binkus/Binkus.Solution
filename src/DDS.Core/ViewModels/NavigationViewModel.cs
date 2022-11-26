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

        // Console.WriteLine($"{UrlPathSegment}");
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
}