namespace DDS.Core.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    // empty ctor for Designer
    public MainWindowViewModel() : this(Globals.GetService<IViewFor<MainViewModel>>()) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainWindowViewModel(IViewFor<MainViewModel> mainView)
    {
        MainView = mainView;
    }

    [ObservableProperty] private IViewFor<MainViewModel>? _mainView;
}