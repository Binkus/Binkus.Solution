namespace DDS.Core.ViewModels;

public sealed partial class MainWindowViewModel : ViewModel
{
    // empty ctor for Designer
    public MainWindowViewModel() : this(Globals.Services, Globals.GetService<IViewFor<MainViewModel>>()) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainWindowViewModel(IServiceProvider services, IViewFor<MainViewModel> mainView) : base(services)
    {
        EnableAsyncInitPrepareActivate = false;
        MainView = mainView;
    }

    [ObservableProperty] private IViewFor<MainViewModel>? _mainView;
}