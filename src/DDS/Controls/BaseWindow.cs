// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using DDS.ViewModels;
using ReactiveUI;

namespace DDS.Controls;

public abstract class BaseWindow<TViewModel> : ReactiveWindow<TViewModel>, IDisposable, IAsyncDisposable
    // , Window, IViewFor<TViewModel>
    where TViewModel : ViewModelBase// new() not needed
{
    public new TViewModel DataContext { get => (TViewModel)base.DataContext!; init => base.DataContext = value; }
    public new TViewModel ViewModel { get => base.ViewModel!; /*protected init => base.ViewModel = value;*/ }
    
    protected BaseWindow(TViewModel viewModel) : this() => base.DataContext = viewModel;
    protected BaseWindow()
    {
        this.WhenActivated(_ =>
        {
            if (base.DataContext is null or not TViewModel) 
                throw new InvalidOperationException($"{nameof(base.DataContext)} of {GetType().Name} is null.");
            // base.DataContext ??= ActivatorUtilities.GetServiceOrCreateInstance<TViewModel>(IAppCore.ServiceProvider);
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

    public IServiceProvider Services { get; protected init; } = Globals.ServiceProvider;
}