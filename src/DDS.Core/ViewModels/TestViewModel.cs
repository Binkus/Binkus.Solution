using DDS.Core.Helper;
using DDS.Core.Services;

namespace DDS.Core.ViewModels;

public partial class TestViewModel : ViewModel
{
    public TestViewModel() : this(Globals.Services) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public TestViewModel(IServiceProvider services) : base(services, 
        Globals.GetService<ServiceScopeManager>().GetMainScope().GetRequiredService<IScreen>()) { }
    
    public string Greeting { get; set; } = $"Hello from Test VM Id:{Guid.NewGuid().ToString()[..8]}";

    [ObservableProperty]
    private string _textBoxContent = "";

    [RelayCommand]
    private void ScopeInvalidation()
    {
        Navigation.To<SecondTestViewModel>();
    }

    protected override async Task OnActivationAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
    }
}

/*
 * todo
 * E.g. lock in ServiceScopeManager, event or Observable OnMainScopeChang{ing,ed}, replacing IServiceProvider references  
 * of disposed IServiceProviders, or something similar.
 * When MainScope is disposed, its ServiceProvider gets disposed, objects referencing those disposed ServiceProviders
 * have to adapt to the change. Mostly affected VMs like this one - Singletons, their HostScreen / NavigationVM.
 */