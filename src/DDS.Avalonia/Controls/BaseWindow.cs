// ReSharper disable MemberCanBePrivate.Global

using System.Reactive.Disposables;
using Avalonia.Input;
using DDS.Core;
using DDS.Core.Services;

namespace DDS.Avalonia.Controls;

public abstract class BaseWindow<TViewModel> : ReactiveWindow<TViewModel>, IReactiveWindowFor<TViewModel>
    where TViewModel : class
{
    public new TViewModel DataContext { get => (TViewModel)base.DataContext!; init => base.DataContext = value; }
    public new TViewModel ViewModel { get => base.ViewModel!; /*protected init => base.ViewModel = value;*/ }
    
    protected BaseWindow(TViewModel viewModel) : this() => base.DataContext = viewModel;
    protected BaseWindow()
    {
        this.WhenActivated(disposables =>
        {
            if (base.DataContext is null or not TViewModel) 
                throw new InvalidOperationException($"{nameof(base.DataContext)} of {GetType().Name} is null.");
            
            HandleActivation();
            Disposable
                .Create(DeactivateView)
                .DisposeWith(disposables);
        });
    }
    
    protected virtual void HandleActivation() {}
    protected virtual void HandleDeactivation() {}
    private void DeactivateView()
    {
        Dispose(true);
        HandleDeactivation();
    }

    protected CompositeDisposable? ViewDisposables { get; private set; } = new();

    private IServiceProvider? _services;
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IServiceProvider Services
    {
        get => _services ??= (_services = (base.DataContext as IProvideServices)?.Services) ?? Globals.Services;
        protected init => _services = value;
    }
    
    public TService GetService<TService>() where TService : notnull => Services.GetRequiredService<TService>();
    
    public object GetService(Type serviceType) => Services.GetRequiredService(serviceType);
    
    public TopLevel GetTopLevel() => this.VisualRoot as TopLevel ?? throw new NullReferenceException("Invalid Owner");
    
    public IInputRoot GetInputRoot() => this.VisualRoot as IInputRoot 
                                                       ?? throw new NullReferenceException("Invalid Owner");

    
    //
    
    protected virtual void Dispose(bool disposing)
    {
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
    //
    // ~BaseWindow()
    // {
    //     Dispose(false);
    // }
}
