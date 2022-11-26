namespace DDS.Core.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    // empty ctor for Designer
    public MainWindowViewModel() : this(Globals.Services, Globals.GetService<IViewFor<MainViewModel>>()) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainWindowViewModel(IServiceProvider services, IViewFor<MainViewModel> mainView) : base(services)
    {
        MainView = mainView;
    }

    [ObservableProperty] private IViewFor<MainViewModel>? _mainView;
}