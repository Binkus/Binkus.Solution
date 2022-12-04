using System.Reactive.Concurrency;
using DDS.Core.Controls;
using DDS.Core.Helper;
using DDS.Core.Services;
using DynamicData.Binding;

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

        GoLogin = NavigateReactiveCommand<LoginViewModel>();
        GoTest = NavigateReactiveCommand<TestViewModel>();
        GoSecondTest = NavigateReactiveCommand<SecondTestViewModel>();
        GoThirdTest = NavigateReactiveCommand<ThirdTestViewModel>();

        Navigation.BackCountOffset++;
        Navigation.ResetTo<LoginViewModel>();
        // AsyncChanges();

        Task.Run(async () =>
        {
            await 5.Seconds();
            GetService<IAppCore>().Post(() => Navigation.BackCountOffset = 0);
        });
        // Task.Run(async () =>
        // {
        //     await 10.Seconds();
        //     GetService<IAppCore>().Post(() => Navigation.BackCountOffset = 0);
        // });


        // Navigation.BackCountOffset = 1;
    }


    protected override Task InitializeAsync(CancellationToken cancellationToken)
    {
        // TeeAsync();
        RxApp.MainThreadScheduler.ScheduleAsync(TeeAsync, (_, func, _) => func());
        // RxApp.MainThreadScheduler.Yield()
        return Task.CompletedTask;
    }

    private async Task TeeAsync()
    {
        for (int i = 1; i <= 20; i++)
        {
            await 500.ms();
            Console.WriteLine($":M:{i}");
        }
    }

    private async void AsyncChanges()
    {
        await 2.Seconds();
        Navigation.BackCountOffset = 0;
        await 2.Seconds();
        Navigation.BackCountOffset = 1;
        await 2.Seconds();
        Navigation.BackCountOffset = 0;
        await 2.Seconds();
        Navigation.BackCountOffset = 1;
        await 2.Seconds();
        Navigation.BackCountOffset = 0;
        await 2.Seconds();
        Navigation.BackCountOffset = 1;
    }
    
    public ReactiveCommand<Unit, IRoutableViewModel> GoLogin { get; }
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