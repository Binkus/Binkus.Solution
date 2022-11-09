namespace DDS.ViewModels;

public class TestViewModel : ViewModelBase
{
    public TestViewModel()
    {
    }
    
    public TestViewModel(IScreen screen) : base(screen)
    {
    }

    public string Greeting { get; set; } = "Hello from ViewModel";
}