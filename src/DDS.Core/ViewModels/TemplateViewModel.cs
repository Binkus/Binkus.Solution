namespace DDS.Core.ViewModels;

[DataContract]
public sealed partial class TemplateViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting = ""; // CommunityToolkit.Mvvm
    
    // private readonly ISomeService _someService;
    
    // empty ctor for Avalonia Designer / View Previewer, IntelliSense for DataContext of View within its axaml
    public TemplateViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public TemplateViewModel(IServiceProvider services) : base(services)
    {
        // _someService = GetService<ISomeService>(); // or through ctor injection
    }

    protected override void HandleActivation()
    {
        
    }
    
    protected override void HandleDeactivation()
    {
        
    }
}