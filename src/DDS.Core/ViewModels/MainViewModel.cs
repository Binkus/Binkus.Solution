using System.Reactive.Concurrency;
using Binkus.ReactiveMvvm;
using DDS.Core.Controls;
using DDS.Core.Helper;
using DDS.Core.Services;
using DynamicData.Binding;

namespace DDS.Core.ViewModels;

public sealed partial class MainViewModel : ViewModel
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
        // EnableAsyncInitPrepareActivate = false;
        
        _avaloniaEssentials = this.GetRequiredService<IAvaloniaEssentials>();

        GoLogin = this.NavigateReactiveCommand<LoginViewModel>();
        GoTest = this.NavigateReactiveCommand<TestViewModel>();
        GoSecondTest = this.NavigateReactiveCommand<SecondTestViewModel>();
        GoThirdTest = this.NavigateReactiveCommand<ThirdTestViewModel>();

        // Navigation.BackCountOffset++;
        // Navigation.ResetTo<LoginViewModel>();
    }
    
    protected override Task InitializeAsync(CancellationToken cancellationToken)
    {
        // TeeAsync();
        // RxApp.MainThreadScheduler.ScheduleAsync(StartAsyncTestAsync, (_, func, _) => func());
        
        Navigation.ResetTo<LoginViewModel>();
        return Task.CompletedTask;
    }

    private static async Task StartAsyncTestAsync()
    {
        for (int i = 1; i <= 20; i++)
        {
            await Task.Delay(500);
            Console.WriteLine($":M:{i}");
        }
    }
    
    public ReactiveCommand<Unit, IRoutableViewModel> GoLogin { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoTest { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoSecondTest { get; }
    public ReactiveCommand<Unit, IRoutableViewModel> GoThirdTest { get; }

    [RelayCommand]
    private async Task OpenFilePickerAsync()
    {
        if (Globals.IsDesignMode) return;
        var fileResult = await _avaloniaEssentials.FilePickerAsync();
        var fullPath = fileResult.FullPath;
        GotPath = fileResult.Exists ? $"fullPath={fullPath}" : "fullPath is empty";
    }

    [RelayCommand]
    private Task OpenDialogAsync()
    {
        var dialog = this.GetRequiredService<IDialogAlertMessageBox>();
        return dialog.ShowAsync(x =>
        {
            x.Title = "Super important question about Navigation:";
            x.Message = "Navigate to Test?";
            x.Button2Text = "Cancel";
            x.Button1Action = () => Navigation.To<TestViewModel>();
        });
    }
}