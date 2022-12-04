using DDS.Core.Helper;

namespace DDS.Core.ViewModels;

[DataContract]
public sealed partial class TemplateExampleViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting; // CommunityToolkit.Mvvm, generates "Greeting"-Property

    private string _examplePropWithBackingFieldSemiManualRaisePropertyChange;
    private string _examplePropWithBackingFieldSemiManualRaisePropertyChange2;
    
    [Reactive, UsedImplicitly]
    public string ReactiveGreeting { get; private set; } // ReactiveUI.Fody

    [UsedImplicitly]
    public string ExamplePropWithBackingFieldSemiManualRaisePropertyChange
    {
        get => _examplePropWithBackingFieldSemiManualRaisePropertyChange;
        set => SetProperty(ref _examplePropWithBackingFieldSemiManualRaisePropertyChange, value);
    }
    
    // Prefer this when manual event rising needed:
    [UsedImplicitly]
    public string ExamplePropWithBackingFieldSemiManualRaisePropertyChange2
    {
        get => _examplePropWithBackingFieldSemiManualRaisePropertyChange2;
        set => this.RaiseAndSetIfChanged(ref _examplePropWithBackingFieldSemiManualRaisePropertyChange2, value);
    }

    // private readonly ISomeService _someService;
    
    // empty ctor for Avalonia Designer / View Previewer, IntelliSense for DataContext of View within its axaml
    public TemplateExampleViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public TemplateExampleViewModel(IServiceProvider services) : base(services)
    {
        _greeting = "Hello";
        ReactiveGreeting = "Hi";
        _examplePropWithBackingFieldSemiManualRaisePropertyChange = "";
        _examplePropWithBackingFieldSemiManualRaisePropertyChange2 = "";

        // _someService = someServiceFromCtor ?? GetService<ISomeService>();
    }
    
    protected override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await 1.Seconds();
    }

    protected override async Task OnPrepareAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        await 1.Seconds();
    }

    protected override Task OnActivationAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        ReactiveGreeting = ":)";
        ExamplePropWithBackingFieldSemiManualRaisePropertyChange2 = "I change on activation (when view is shown)";
        return Task.CompletedTask;
    }
    
    protected override void OnDeactivation()
    {
        Console.WriteLine("When View disappears from view");
        CwGreetings();
    }

    private void CwGreetings()
    {
        Console.WriteLine((string?)Greeting);
        Console.WriteLine(ReactiveGreeting);
    }
}