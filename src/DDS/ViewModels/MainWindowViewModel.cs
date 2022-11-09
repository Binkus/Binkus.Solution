namespace DDS.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(MainView mainView)
    {
        MainView = mainView;
    }
    
    [Reactive] public ReactiveUserControl<MainViewModel>? MainView { get; set; }
}