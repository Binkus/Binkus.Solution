// ReSharper disable MemberCanBePrivate.Global

namespace DDS.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IRoutableViewModel, IActivatableViewModel
{
    public string? UrlPathSegment { get; }

    private IScreen? _hostScreen;
    
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IScreen HostScreen
    {
        get => _hostScreen ??= Services.GetRequiredService<IScreen>();
        protected init => this.RaiseAndSetIfChanged(ref _hostScreen, value);
    }

    public ViewModelActivator Activator { get; } = new();
    
    public string ViewModelName { get; private init; }
    private string? _customViewName;
    public string CustomViewName
    {
        get => _customViewName ??= ViewModelName.EndsWith("ViewModel") ? ViewModelName[..^9] : ViewModelName;
        set => this.RaiseAndSetIfChanged(ref _customViewName, value);
    }

    protected ViewModelBase(IScreen hostScreen) : this() => _hostScreen = hostScreen;
    
    protected ViewModelBase()
    {
        ViewModelName = GetType().UnderlyingSystemType.Name;
        UrlPathSegment = $"/{CustomViewName.ToLowerInvariant()}";

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