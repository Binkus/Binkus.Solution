using DDS.Core.Helper;

namespace DDS.Core.ViewModels;

[DataContract]
public sealed partial class TemplateViewModel : ViewModel
{
    [ObservableProperty] private string _greeting = ""; // CommunityToolkit.Mvvm
    
    // private readonly ISomeService _someService;
    
    // empty ctor for Avalonia Designer / View Previewer, IntelliSense for DataContext of View within its axaml
    public TemplateViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public TemplateViewModel(IServiceProvider services) : base(services)
    {
        // _someService = someServiceFromCtor ?? GetService<ISomeService>();

        Greeting = "Property is available because of [ObservableProperty] Attribute and Mvvm Toolkit Source Generator";
    }

    protected override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
    }

    protected override async Task OnActivationAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
    }
    
    protected override void OnDeactivation()
    {
        
    }
}