namespace Binkus.ReactiveMvvm;

public static class ViewModelExtensions
{
    #region Simplified Command Creation

    public static ReactiveCommand<Unit, IRoutableViewModel> NavigateReactiveCommand<TViewModel>(this IViewModel viewModel, IObservable<bool>? canExecute = default)
        where TViewModel : class, IRoutableViewModel 
        => CreateNavigationReactiveCommandFromObservable<TViewModel>(viewModel,
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => viewModel.Navigation.Router.Navigate), canExecute);
    
    public static ReactiveCommand<Unit, IRoutableViewModel> NavigateAndResetReactiveCommand<TViewModel>(this IViewModel viewModel, IObservable<bool>? canExecute = default)
        where TViewModel : class, IRoutableViewModel 
        => CreateNavigationReactiveCommandFromObservable<TViewModel>(viewModel,
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => viewModel.Navigation.Router.NavigateAndReset), canExecute);

    public static ReactiveCommand<Unit, IRoutableViewModel> CreateNavigationReactiveCommandFromObservable<TViewModel>(this IViewModel viewModel,
        Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>> navi, IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel
        => ReactiveCommand.CreateFromObservable(
            () => navi.Value.Execute(viewModel.GetRequiredService<TViewModel>()),
            // todo evaluate making even more lazy, can execute can load values, when returned cmd is e.g. used as bound cmd to view
            canExecute: canExecute ?? viewModel.WhenAnyObservable(x => x.Navigation.Router.CurrentViewModel).Select(x => x is not TViewModel)
        );
    
    //

    public static ReactiveCommand<Unit, IRoutableViewModel> NavigateReactiveCommand(this IViewModel viewModel, Type viewModelType, IObservable<bool>? canExecute = default)
        => CreateNavigationReactiveCommandFromObservable(viewModel, viewModelType, 
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => viewModel.Navigation.Router.Navigate), canExecute);
    
    public static ReactiveCommand<Unit, IRoutableViewModel> NavigateAndResetReactiveCommand(this IViewModel viewModel, Type viewModelType, IObservable<bool>? canExecute = default)
        => CreateNavigationReactiveCommandFromObservable(viewModel, viewModelType, 
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => viewModel.Navigation.Router.NavigateAndReset),canExecute);
    
    public static ReactiveCommand<Unit, IRoutableViewModel> CreateNavigationReactiveCommandFromObservable(this IViewModel viewModel, Type viewModelType,
        Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>> navi, IObservable<bool>? canExecute = default)
    {
        if (viewModelType.IsAssignableTo(typeof(IRoutableViewModel)) is false) throw new InvalidOperationException();
        return ReactiveCommand.CreateFromObservable(
            () => navi.Value.Execute((IRoutableViewModel)viewModel.GetRequiredService(viewModelType)),
            // todo evaluate making even more lazy, can execute can load values, when returned cmd is e.g. used as bound cmd to view
            canExecute: canExecute ?? viewModel.WhenAnyObservable(x => x.Navigation.Router.CurrentViewModel)
                .Select(x => !x?.GetType().IsAssignableTo(viewModelType) ?? true)
        );
    }
    
    #endregion
}