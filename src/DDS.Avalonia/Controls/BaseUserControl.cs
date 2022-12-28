using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.Input;
using Binkus.ReactiveMvvm;
using DDS.Core;
using DDS.Core.Services;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace DDS.Avalonia.Controls;

public abstract class BaseUserControl<TViewModel> : ReactiveUserControl<TViewModel>, IReactiveViewFor<TViewModel>
    where TViewModel : class
{
    public new TViewModel DataContext { get => (TViewModel)base.DataContext!; init => base.DataContext = value; }
    public new TViewModel ViewModel { get => base.ViewModel!; /*protected init => base.ViewModel = value;*/ }

    public Guid Id { get; } = Guid.NewGuid();
    
    public bool DisposeWhenActivatedSubscription { get; set; }
    public bool DisposeOnDeactivation { get; set; }
    
    protected BaseUserControl(TViewModel viewModel) : this() => base.DataContext = viewModel;
    protected BaseUserControl()
    {
        IDisposable? subscription = null;
        subscription = this.WhenActivated(disposables =>
        {
            if (base.DataContext is null or not TViewModel) 
                throw new InvalidOperationException($"{nameof(base.DataContext)} of {GetType().Name} is null.");

            Debug.Write($"    |_ {(DataContext as ViewModel)?.ViewModelName} _ View Activated\n");

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
        

        Debug.WriteLine("cv:"+ this.GetType().UnderlyingSystemType.Name);
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
    
    protected CompositeDisposable? ViewDisposables { get; private set; } = new();

    private IServiceProvider? _services;
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IServiceProvider Services
    {
        get => _services ??= (_services = (base.DataContext as IProvideServices)?.Services)
                             ?? throw new InvalidOperationException($"{nameof(base.DataContext)} "
                                                                    + $"of {GetType().Name} is null.");
        protected init => _services = value;
    }
    
    public TService GetService<TService>() where TService : notnull => Services.GetRequiredService<TService>();
    
    public object GetService(Type serviceType) => Services.GetRequiredService(serviceType);
    
    // public Window GetWindow() => this.VisualRoot as Window ?? throw new NullReferenceException("Invalid Owner");
    public TopLevel GetTopLevel() => this.VisualRoot as TopLevel ?? throw new NullReferenceException("Invalid Owner");
    public IInputRoot GetInputRoot() => this.VisualRoot as IInputRoot 
                                                ?? throw new NullReferenceException("Invalid Owner");
    
    
    //
    
    protected virtual void Dispose(bool disposing)
    {
        Debug.WriteLine("Dv:"+ this.GetType().UnderlyingSystemType.Name+",disposing="+disposing);
        if (disposing)
        {
            ViewDisposables?.Dispose();
            ViewDisposables = null;
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

    // ~BaseUserControl()
    // {
    //     Dispose(false);
    //     // Debug.WriteLine("DFv:"+ this.GetType().UnderlyingSystemType.Name);
    // }
}
