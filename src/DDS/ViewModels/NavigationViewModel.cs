namespace DDS.ViewModels;

public class NavigationViewModel : ViewModelBase, IScreen
{
    public override RoutingState Router { get; } = new();
    
    public ReactiveCommand<Unit, IRoutableViewModel?> GoBack { get; }

    public NavigationViewModel()
    {
        var canGoBack = this
            .WhenAnyValue(x => x.Router.NavigationStack.Count)
            .Select(count => count > 0);
        GoBack = ReactiveCommand.CreateFromObservable(
            () => Router.NavigateBack.Execute(Unit.Default),
            canGoBack);
    }
}