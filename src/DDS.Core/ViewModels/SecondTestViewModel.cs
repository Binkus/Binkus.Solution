namespace DDS.Core.ViewModels;

public class SecondTestViewModel : ViewModelBase
{
    public string Greeting { get; set; } = $"Hello from 2ndT VM Id:{Guid.NewGuid().ToString()[..8]}";
}