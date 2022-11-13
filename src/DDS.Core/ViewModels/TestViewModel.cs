namespace DDS.Avalonia.ViewModels;

public class TestViewModel : ViewModelBase
{
    public string Greeting { get; set; } = $"Hello from Test VM Id:{Guid.NewGuid().ToString()[..8]}";
}