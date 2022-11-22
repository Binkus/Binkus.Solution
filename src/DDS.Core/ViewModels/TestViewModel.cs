namespace DDS.Core.ViewModels;

public partial class TestViewModel : ViewModelBase
{
    public TestViewModel() : this(Globals.Services) { }
    
    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public TestViewModel(IServiceProvider services) : base(services) { }
    
    public string Greeting { get; set; } = $"Hello from Test VM Id:{Guid.NewGuid().ToString()[..8]}";

    [ObservableProperty]
    private string _textBoxContent = "";
}