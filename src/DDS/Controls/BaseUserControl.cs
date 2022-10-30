using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using DDS.ViewModels;
using ReactiveUI;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace DDS.Controls;

public abstract class 
    BaseUserControl<TViewModel> : ReactiveUserControl<TViewModel>
    // , UserControl, IViewFor<TViewModel> 
    where TViewModel : ViewModelBase //, new() // TViewModel : class // new() not needed
{
    public new TViewModel DataContext { get => (TViewModel)base.DataContext!; set => base.DataContext = value; }
    public new TViewModel ViewModel { get => base.ViewModel!; /*protected init => base.ViewModel = value;*/ }

    protected BaseUserControl(TViewModel viewModel) : this() => base.DataContext = viewModel;
    protected BaseUserControl()
    {
        this.WhenActivated(_ =>
        {
            if (base.DataContext is null or not TViewModel) 
                throw new InvalidOperationException($"{nameof(base.DataContext)} of {GetType().Name} is null.");
        }).DisposeWith(ViewDisposables!);
    }
    
    protected CompositeDisposable? ViewDisposables { get; private set; } = new();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ViewDisposables?.Dispose();
            ViewDisposables = null;
            // base.ViewModel?.Dispose();
            base.ViewModel = null;
            base.DataContext = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
    
    Window GetWindow() => this.VisualRoot as Window ?? throw new NullReferenceException("Invalid Owner");
    TopLevel GetTopLevel() => this.VisualRoot as TopLevel ?? throw new NullReferenceException("Invalid Owner");
    Avalonia.Input.IInputRoot GetInputRoot() => this.VisualRoot as Avalonia.Input.IInputRoot 
                                                ?? throw new NullReferenceException("Invalid Owner");
}
