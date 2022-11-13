namespace DDS.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IAvaloniaEssentials _avaloniaEssentials;

    public string Greeting => "Greetings from MainView";

    [ObservableProperty] 
    private string _gotPath = "fullPath is empty";

    // Necessary for Designer:
    public MainViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainViewModel(IServiceProvider services) : base(services)
    {
        _avaloniaEssentials = GetService<IAvaloniaEssentials>();

        GoTest = NavigateReactiveCommand<TestViewModel>();
        GoSecondTest = NavigateReactiveCommand<SecondTestViewModel>();
    }
    
    public ReactiveCommand<Unit, IRoutableViewModel> GoTest { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoSecondTest { get; }

    [RelayCommand]
    async Task OpenFilePicker()
    {
        if (Globals.IsDesignMode) return;
        var fileResult = await _avaloniaEssentials.FilePickerAsync();
        var fullPath = fileResult.FullPath;
        GotPath = fileResult.Exists ? $"fullPath={fullPath}" : "fullPath is empty";
    }

    [RelayCommand]
    Task OpenDialog()
    {
        return Task.CompletedTask;

    }
}