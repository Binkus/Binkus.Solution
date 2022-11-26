namespace DDS.Core.ViewModels;

public interface INavigationViewModel<TForViewModel> : INavigationViewModel, IViewModelBase<TForViewModel> 
    where TForViewModel : class, IViewModel
{ }

public interface INavigationViewModel : IScreen, IViewModelBase
{
    /// <summary>
    /// <inheritdoc cref="IScreen.Router"/>
    /// <p>Contains the navigation stack.</p>
    /// </summary>
    new RoutingState Router { get; }

    /// <summary>
    /// Command for navigating back
    /// </summary>
    ReactiveCommand<Unit, IRoutableViewModel?> BackCommand { get; set; }
    
    /// <summary>
    /// Observable used by canExecute of BackCommand to determine if going back is allowed or not
    /// </summary>
    IObservable<bool> CanGoBack { get; set; }
    
    int BackCountOffset { get; set; }
    
    int StackCount { get; }
    
    /// <summary>
    /// Navigates back by executing BackCommand when it can execute
    /// </summary>
    /// <returns>true when navigation successful, otherwise false</returns>
    bool Back();
    
    /// <summary>
    /// Navigates to viewModelType when IRoutableViewModel
    /// </summary>
    /// <param name="viewModelType">IRoutableViewModel to navigate to</param>
    /// <param name="canExecute">Override default behaviour of canExecute parameter</param>
    /// <returns>true when navigation successful, otherwise false</returns>
    bool To(Type viewModelType, IObservable<bool>? canExecute = default);
    
    /// <summary>
    /// Navigates to TViewModel
    /// </summary>
    /// <param name="canExecute">Override default behaviour of canExecute parameter</param>
    /// <typeparam name="TViewModel">IRoutableViewModel to navigate to</typeparam>
    /// <returns>true when navigation successful, otherwise false</returns>
    bool To<TViewModel>(IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel;
    
    /// <summary>
    /// Navigates to TViewModel and resets navigation stack
    /// </summary>
    /// <param name="canExecute">Override default behaviour of canExecute parameter</param>
    /// <typeparam name="TViewModel">IRoutableViewModel to navigate to</typeparam>
    /// <returns>true when navigation successful, otherwise false</returns>
    bool ResetTo<TViewModel>(IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel;
    
    /// <summary>
    /// Navigates to viewModelType when IRoutableViewModel and resets navigation stack
    /// </summary>
    /// <param name="viewModelType">IRoutableViewModel to navigate to</param>
    /// <param name="canExecute">Override default behaviour of canExecute parameter</param>
    /// <returns>true when navigation successful, otherwise false</returns>
    bool ResetTo(Type viewModelType, IObservable<bool>? canExecute = default);
    
    /// <summary>
    /// Resets navigation stack of RoutingState Router
    /// </summary>
    void Reset();
}