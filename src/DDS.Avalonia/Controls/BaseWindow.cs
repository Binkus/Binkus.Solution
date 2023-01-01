// ReSharper disable MemberCanBePrivate.Global

using System.Reactive.Disposables;
using Avalonia.Input;
using Binkus.ReactiveMvvm;
using DDS.Core;
using DDS.Core.Services;
using ReactiveUI;

namespace DDS.Avalonia.Controls;

public abstract class BaseWindow<TViewModel> : ReactiveWindow<TViewModel>, IReactiveWindowFor<TViewModel>
    where TViewModel : class
{
    public new TViewModel DataContext { get => (TViewModel)base.DataContext!; init => base.DataContext = value; }
    public new TViewModel ViewModel { get => base.ViewModel!; /*protected init => base.ViewModel = value;*/ }
    
    public bool DisposeWhenActivatedSubscription { get; set; }

    public bool DisposeOnDeactivation { get; set; } = true;
    
    protected BaseWindow(TViewModel viewModel) : this() => base.DataContext = viewModel;
    protected BaseWindow()
    {
        IDisposable? subscription = null;
        subscription = this.WhenActivated(disposables =>
        {
            if (base.DataContext is null or not TViewModel) 
                throw new InvalidOperationException($"{nameof(base.DataContext)} of {GetType().Name} is null.");
            
            (DataContext as ViewModel)?.OnViewActivation(disposables);
            HandleActivation();
            Disposable
                .Create(DisposeOnDeactivation ? DisposeView : HandleDeactivationBase)
                .DisposeWith(disposables);

            if (DisposeWhenActivatedSubscription)
            {
                // ReSharper disable once AccessToModifiedClosure
                subscription?.DisposeWith(disposables);
            }
        });
    }
    
    protected virtual void HandleActivation() {}
    protected virtual void HandleDeactivation() {}
    private void HandleDeactivationBase()
    {
        (DataContext as ViewModel)?.OnViewDeactivation();
        HandleDeactivation();
    }
    private void DisposeView()
    {
        HandleDeactivationBase();
        (DataContext as ViewModel)?.OnViewDisposal();
        Dispose(true);
    }

    private IServiceProvider? _services;
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IServiceProvider Services
    {
        get => _services ??= (_services = (base.DataContext as IProvideServices)?.Services
                                          ?? base.DataContext as IServiceProvider)
                             ?? throw new InvalidOperationException($"{nameof(base.DataContext)} "
                                                                    + $"of {GetType().Name} is null.");
        protected init => _services = value;
    }
    
    public TopLevel GetTopLevel() => this.VisualRoot as TopLevel ?? throw new NullReferenceException("Invalid Owner");
    
    public IInputRoot GetInputRoot() => this.VisualRoot as IInputRoot 
                                                       ?? throw new NullReferenceException("Invalid Owner");

    
    //
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            base.ViewModel = null;
            base.DataContext = null;
        }
    }

    // public void Dispose()
    // {
    //     Dispose(true);
    //     GC.SuppressFinalize(this);
    // }
    //
    // public ValueTask DisposeAsync()
    // {
    //     Dispose();
    //     return default;
    // }
    //
    // ~BaseWindow()
    // {
    //     Dispose(false);
    // }
}
