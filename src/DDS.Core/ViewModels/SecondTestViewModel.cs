namespace DDS.Core.ViewModels;

public partial class SecondTestViewModel : ViewModelBase
{
    public SecondTestViewModel() : this(Globals.Services) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public SecondTestViewModel(IServiceProvider services) : base(services) { }
    
    public string Greeting { get; set; } = $"Hello from 2ndT VM Id:{Guid.NewGuid().ToString()[..8]}";
    
    [ObservableProperty]
    private string _textBoxContent = "";
}