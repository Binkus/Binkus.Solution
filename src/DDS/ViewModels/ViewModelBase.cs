using System.Reactive.Disposables;

// ReSharper disable MemberCanBePrivate.Global

namespace DDS.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IRoutableViewModel, IActivatableViewModel
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

    protected ViewModelBase(IScreen? hostScreen)
    {
        ViewModelName = this.GetType().UnderlyingSystemType.Name;
        UrlPathSegment = $"/{CustomViewName.ToLowerInvariant()}";
        HostScreen = hostScreen!;
        
        this.WhenActivated(disposables => 
        {
            Debug.WriteLine(UrlPathSegment + ":");

            HandleActivation();
            Disposable
                .Create(HandleDeactivation)
                .DisposeWith(disposables);
        });
        
        Debug.WriteLine("c:"+ViewModelName);
    }
    
    protected virtual void HandleActivation() { }
    protected virtual void HandleDeactivation() { }
    
    public IServiceProvider Services { get; protected init; } = Globals.ServiceProvider;
}