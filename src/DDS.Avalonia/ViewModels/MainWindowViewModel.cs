namespace DDS.Avalonia.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    // empty ctor for Designer
    public MainWindowViewModel() : this(Globals.GetService<MainView>()) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainWindowViewModel(MainView mainView)
    {
        MainView = mainView;
    }

    [ObservableProperty] private ReactiveUserControl<MainViewModel>? _mainView;
}