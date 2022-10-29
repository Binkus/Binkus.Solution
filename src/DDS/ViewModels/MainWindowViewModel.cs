using ReactiveUI.Fody.Helpers;

namespace DDS.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public ReactiveUserControl<MainViewModel>? MainView { get; set; }
}