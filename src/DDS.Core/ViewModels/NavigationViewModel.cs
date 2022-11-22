using System.Windows.Input;

namespace DDS.Core.ViewModels;

public class NavigationViewModel : ViewModelBase, IScreen
{
    [IgnoreDataMember]
    public sealed override RoutingState Router { get; } = new();
    
    [IgnoreDataMember]
    public ReactiveCommand<Unit, IRoutableViewModel?> BackCommand { get; }

    [IgnoreDataMember]
    public sealed override IScreen HostScreen { get => base.HostScreen; protected init => base.HostScreen = value; }

    public NavigationViewModel() : this(Globals.Services) { }
    
    [ActivatorUtilitiesConstructor]
    public NavigationViewModel(IServiceProvider services) : base(services)
    {
        HostScreen = this;
        
        var canGoBack = this
            .WhenAnyValue(x => x.Router.NavigationStack.Count)
            .Select(count => count > 0);
        BackCommand = ReactiveCommand.CreateFromObservable(
            () => Router.NavigateBack.Execute(Unit.Default),
            canGoBack);
    }

    public bool Navigate<TViewModel>() where TViewModel : class, IRoutableViewModel
    {
        using var cmd = NavigateReactiveCommand<TViewModel>();
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).Subscribe();
        return true;
    }
    
    public bool NavigateAndReset<TViewModel>() where TViewModel : class, IRoutableViewModel
    {
        using var cmd = NavigateAndResetReactiveCommand<TViewModel>();
        if (((ICommand)cmd).CanExecute(null) is false) return false;
        cmd.Execute(Unit.Default).Subscribe();
        return true;
    }
}