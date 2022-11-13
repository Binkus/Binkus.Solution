namespace DDS.Avalonia.ViewModels;

[DataContract]
public sealed partial class TemplateExampleViewModel : ViewModelBase
{
    [ObservableProperty] private string _greeting; // CommunityToolkit.Mvvm

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
        // Initializing non-nullable Greeting property through property itself generated by [ObservableProperty] throws
        // an error when NullableAsErrors, seems to be not well supported for partial classes, cause no auto setter,
        // same when manual setter like in ExamplePropWithBackingFieldSemiManualRaisePropertyChange*:
        // "[CS8618] Non-nullable field '_greeting' must contain a non-null value when exiting constructor.
        // Consider declaring the field as nullable."
        // Greeting = "Hello";
        _greeting = "Hello";
        ReactiveGreeting = "Hi"; // Auto Property Setter works
        _examplePropWithBackingFieldSemiManualRaisePropertyChange = "";
        _examplePropWithBackingFieldSemiManualRaisePropertyChange2 = "";

        // _someService = GetService<ISomeService>(); // or through ctor injection
    }

    protected override void HandleActivation()
    {
        ReactiveGreeting = ":)";
        ExamplePropWithBackingFieldSemiManualRaisePropertyChange2 = "I change on activation (when view is shown)";
    }
    
    protected override void HandleDeactivation()
    {
        Console.WriteLine("When View disappears from view");
        CwGreetings();
    }

    private void CwGreetings()
    {
        Console.WriteLine(Greeting);
        Console.WriteLine(ReactiveGreeting);
    }
}