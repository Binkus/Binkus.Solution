using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace DDS.ViewModels;

public class ViewModelBase : ReactiveObject, IRoutableViewModel, IActivatableViewModel
{
    public string? UrlPathSegment { get; }
    public IScreen HostScreen { get; init; }
    public ViewModelActivator Activator { get; } = new();
    
    public string ViewModelName { get; private init; }
    private string? _customViewName;
    public string CustomViewName
    {
        get => _customViewName ??= ViewModelName.EndsWith("ViewModel") ? ViewModelName[..^9] : ViewModelName;
        set => this.RaiseAndSetIfChanged(ref _customViewName, value);
    }
    
    protected ViewModelBase() : this(default) { }
    public ViewModelBase(IScreen? hostScreen)
    {
        ViewModelName = this.GetType().UnderlyingSystemType.Name;
        UrlPathSegment = $"/{CustomViewName.ToLowerInvariant()}";
        HostScreen = hostScreen!;
        
        this.WhenActivated((CompositeDisposable disposables) =>
        {
            /* handle activation */
            Disposable
                .Create(() => { /* handle deactivation */ })
                .DisposeWith(disposables);
        });
        
        this.WhenActivated(disposables => 
        {
#if DEBUG
            Console.WriteLine(UrlPathSegment + ":");
#endif
            HandleActivation();
            Disposable
                .Create(HandleDeactivation)
                .DisposeWith(disposables);
        });
    }
    
    protected virtual void HandleActivation() { }
    protected virtual void HandleDeactivation() { }
}