using Binkus.ReactiveMvvm;

namespace DDS.Core.ViewModels;

public partial class SecondTestViewModel : ViewModel
{
    public SecondTestViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public SecondTestViewModel(IServiceProvider services) : base(services)
    {
        SecondNavigation = services.CreateScope().ServiceProvider
            .GetRequiredService<INavigationViewModel<SecondTestViewModel>>();
        
        // NavigateToTestViewModelCommand = SecondNavigation.NavigateReactiveCommand<ThirdTestViewModel>();
        NavigateToTestViewModelCommand = NavigateReactiveCommand<MainViewModel>(SecondNavigation);

    }
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateToTestViewModelCommand { get; } 
    
    public string Greeting { get; set; } = $"Hello from 2ndT VM Id:{Guid.NewGuid().ToString()[..8]}";
    
    [ObservableProperty]
    private string _textBoxContent = "";

    public INavigationViewModel<SecondTestViewModel> SecondNavigation { get; }

    [RelayCommand]
    private void GoSomewhere()
    {
        SecondNavigation.To<ThirdTestViewModel>();
    }
}