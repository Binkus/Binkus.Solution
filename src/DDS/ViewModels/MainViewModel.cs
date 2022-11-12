namespace DDS.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IAvaloniaEssentials _avaloniaEssentials;

    public string Greeting => "Greetings from MainView";

    [ObservableProperty] 
    private string _gotPath = "fullPath is empty";

    // public NavigationViewModel Navigation { get; }

    // Necessary for Designer:
    public MainViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainViewModel(IServiceProvider services
        // , NavigationViewModel navigation, IAvaloniaEssentials? avaloniaEssentials, 
        // Lazy<TestViewModel> testViewModel
        ) : base(services)
    {
        // Services = services;
        _avaloniaEssentials = GetService<IAvaloniaEssentials>()!;
        // _avaloniaEssentials ??= avaloniaEssentials ?? GetService<IAvaloniaEssentials>()!;

        // HostScreen = Navigation = navigation;

        // GoTest = ReactiveCommand.CreateFromObservable(
        //     () => Router.Navigate.Execute(GetService<TestViewModel>()),
        //     canExecute: this.WhenAnyObservable(x => x.Router.CurrentViewModel).Select(x => x is not TestViewModel)
        // );
        // GoSecondTest = ReactiveCommand.CreateFromObservable(
        //     () => Router.Navigate.Execute(GetService<SecondTestViewModel>()), // Transient needs resolvation each cmd execution
        //     canExecute: this.WhenAnyObservable(x => x.Router.CurrentViewModel).Select(x => x is not SecondTestViewModel)
        // );

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
    void OpenDialog()
    {

    }
}