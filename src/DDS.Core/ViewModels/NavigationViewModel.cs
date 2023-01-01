using System.Windows.Input;
using Binkus.ReactiveMvvm;
using CommunityToolkit.Mvvm.DependencyInjection;
// using DDS.Core.Helper;
using DynamicData.Binding;

namespace DDS.Core.ViewModels;

public sealed class NavigationViewModel : NavigationViewModelBase<IViewModel>
{
    public NavigationViewModel() : this(Ioc.Default) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public NavigationViewModel(IServiceProvider services) : base(services) { }
}

public sealed class NavigationViewModel<TForViewModel> : NavigationViewModelBase<TForViewModel>
    where TForViewModel : class, IViewModel
{
    public NavigationViewModel() : this(Ioc.Default) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public NavigationViewModel(IServiceProvider services) : base(services) { }
}

public abstract partial class NavigationViewModelBase<TForViewModel> : ViewModel<TForViewModel>, INavigationViewModel<TForViewModel>
    where TForViewModel : class, IViewModel
{
    [IgnoreDataMember]
    public RoutingState Router { get; } = new();

    [ObservableProperty, IgnoreDataMember]
    private bool _isCurrentViewEnabled = true;

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

    [ObservableProperty, IgnoreDataMember] private int _backCountOffset;

    [ObservableProperty, IgnoreDataMember] private int _stackCount;

    [ObservableProperty, IgnoreDataMember] private bool _canGoBackBool;

    /// <summary>
    /// <p>Returns this.</p>
    /// <inheritdoc />
    /// </summary>
    [IgnoreDataMember]
    public sealed override IScreen HostScreen => this;

    protected NavigationViewModelBase(IServiceProvider services) : base(services)
    {
        EnableAsyncInitPrepareActivate = false;
        
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

        this.WhenAnyObservable(x => x.Router.CurrentViewModel)
            .Do(_ => IsCurrentViewEnabled = true)
            .Where(x => x is ViewModel).Select(x => (ViewModel)x!)
            .Select(x => x.WhenAnyValue(vm => vm.IsActivated))
            .Switch().Subscribe(b => IsCurrentViewEnabled = b);
    }
    
    public void Reset() => Router.NavigationStack.Clear();
    
    public bool Back() => BackCommand.ExecuteIfExecutable();

    // Safe generic navigation

    public bool To<TViewModel>(IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel
    {
        using var cmd = this.NavigateReactiveCommand<TViewModel>(canExecute);
        return cmd.ExecuteIfExecutable();
    }
    
    public bool ResetTo<TViewModel>(IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel
    {
        using var cmd = this.NavigateAndResetReactiveCommand<TViewModel>(canExecute);
        return cmd.ExecuteIfExecutable();
    }
    
    // Navigation to runtime types
    
    public bool ResetTo(Type viewModelType, IObservable<bool>? canExecute = default)
    {
        using var cmd = this.NavigateAndResetReactiveCommand(viewModelType, canExecute);
        return cmd.ExecuteIfExecutable();
    }
    
    public bool To(Type viewModelType, IObservable<bool>? canExecute = default)
    {
        using var cmd = this.NavigateReactiveCommand(viewModelType, canExecute);
        return cmd.ExecuteIfExecutable();
    }
}

// using DDS.Core.Helper; temp alternative:
file static class Helper
{
    private static IDisposable SubscribeAndDisposeOnNext<T>(this IObservable<T> source, Action<T>? onNext = default)
    {
        IDisposable? subscription = null;
        // ReSharper disable once AccessToModifiedClosure
        subscription = source.Subscribe(x =>
        {
            onNext?.Invoke(x);
            subscription?.Dispose();
        });
        return subscription;
    }

    private static bool CanExecute(this ICommand command)
    {
        return command.CanExecute(null);
    }
    
    internal static bool ExecuteIfExecutable<TResult>(this ReactiveCommandBase<Unit, TResult> cmd)
    {
        try
        {
            if (cmd.CanExecute() is false) return false;
            cmd.Execute(Unit.Default).SubscribeAndDisposeOnNext();
            return true;
        }
        catch (ReactiveUI.UnhandledErrorException e)
        {
            Debug.WriteLine(e);
            throw e.InnerException ?? e;
        }
    }
}