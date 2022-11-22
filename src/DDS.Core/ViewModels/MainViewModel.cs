using DDS.Core.Controls;
using DDS.Core.Services;

namespace DDS.Core.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IAvaloniaEssentials _avaloniaEssentials;

    public string Greeting => "Greetings from MainView";

    [ObservableProperty] 
    private string _gotPath = "fullPath is empty";

    // Necessary for Designer:
    public MainViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainViewModel(IServiceProvider services) : base(services)
    {
        _avaloniaEssentials = GetService<IAvaloniaEssentials>();

        GoTest = NavigateReactiveCommand<TestViewModel>();
        GoSecondTest = NavigateReactiveCommand<SecondTestViewModel>();
        GoThirdTest = NavigateReactiveCommand<ThirdTestViewModel>();
    }
    
    public ReactiveCommand<Unit, IRoutableViewModel> GoTest { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoSecondTest { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoThirdTest { get; }

    [RelayCommand]
    private async Task OpenFilePicker()
    {
        if (Globals.IsDesignMode) return;
        var fileResult = await _avaloniaEssentials.FilePickerAsync();
        var fullPath = fileResult.FullPath;
        GotPath = fileResult.Exists ? $"fullPath={fullPath}" : "fullPath is empty";
    }

    [RelayCommand]
    private Task OpenDialog()
    {
        var dialog = GetService<IDialogAlertMessageBox>();
        return dialog.ShowAsync(x =>
        {
            x.Title = "Super important question about Navigation:";
            x.Message = "Navigate to Test?";
            x.Button2Text = "Cancel";
            x.Button1Action = () => Navigation.To<TestViewModel>();
        });
    }
}