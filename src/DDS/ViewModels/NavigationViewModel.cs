namespace DDS.ViewModels;

public class NavigationViewModel : ViewModelBase, IScreen
{
    [IgnoreDataMember]
    public sealed override RoutingState Router { get; } = new();
    
    [IgnoreDataMember]
    public ReactiveCommand<Unit, IRoutableViewModel?> GoBack { get; }

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
        GoBack = ReactiveCommand.CreateFromObservable(
            () => Router.NavigateBack.Execute(Unit.Default),
            canGoBack);
    }
}