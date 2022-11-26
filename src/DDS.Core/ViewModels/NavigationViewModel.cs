using System.Windows.Input;

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
        
        _canGoBack = this
            .WhenAnyValue(x => x.Router.NavigationStack.Count)
            .Select(count => count > 0);
        _backCommand = ReactiveCommand.CreateFromObservable(
            () => Router.NavigateBack.Execute(Unit.Default),
            CanGoBack);
    }
    
    public void Reset() => Router.NavigationStack.Clear();
    
    public bool Back()
    {
        if (((ICommand)BackCommand).CanExecute(null) is false) return false;
        BackCommand.Execute(Unit.Default).Subscribe();
        return true;
    }
    
    // Safe generic navigation

    public bool To<TViewModel>() where TViewModel : class, IRoutableViewModel
    {
        using var cmd = NavigateReactiveCommand<TViewModel>();
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).Subscribe();
        return true;
    }
    
    public bool ResetTo<TViewModel>() where TViewModel : class, IRoutableViewModel
    {
        using var cmd = NavigateAndResetReactiveCommand<TViewModel>();
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).Subscribe();
        return true;
    }
    
    // Navigation to runtime types
    
    public bool ResetTo(Type viewModelType)
    {
        using var cmd = NavigateAndResetReactiveCommand(viewModelType);
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).Subscribe();
        return true;
    }
    
    public bool To(Type viewModelType)
    {
        using var cmd = NavigateReactiveCommand(viewModelType);
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).Subscribe();
        return true;
    }
}