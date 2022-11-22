namespace DDS.Core.ViewModels;

public partial class ThirdTestViewModel : ViewModelBase
{
    public ThirdTestViewModel() : this(Globals.Services) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public ThirdTestViewModel(IServiceProvider services) : base(services) { }
    
    public string Greeting { get; set; } = $"Hello from 3ndT VM Id:{Guid.NewGuid().ToString()[..8]}";
    
    [ObservableProperty]
    private string _textBoxContent = "";
}