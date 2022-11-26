using DDS.Core.Helper;
using DDS.Core.Services;

namespace DDS.Core.ViewModels;

[DataContract]
public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly ILoginService _loginService;
    public LoginViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public LoginViewModel(IServiceProvider services, ILoginService? loginService = null) : base(services)
    {
        _loginService = loginService ?? GetService<ILoginService>();

        IObservable<bool> canLogin = this.WhenAnyValue(
            x => x.LoginName, x => x.Password,
            (loginName, password)
                => !string.IsNullOrWhiteSpace(loginName) && !string.IsNullOrWhiteSpace(password));
        TryLoginCommand = ReactiveCommand.CreateFromTask(
            execute: TryLogin,
            canExecute: canLogin);
        TryRegisterCommand = ReactiveCommand.CreateFromTask(
            execute: TryRegister,
            canExecute: canLogin);
    }
    

    protected override void HandleActivation()
    {
        Console.WriteLine("Login Activation");
        if (_loginService.IsLoggedIn)
        {
            // Navigation.To<TestViewModel>();
            // Nav2();
        }
    }

    async void Nav2()
    {
        await 4.Seconds();
        Navigation.To<SecondTestViewModel>();
    }
    
    protected override void HandleDeactivation()
    {
        
    }

    [Reactive] public string LoginName { get; set; } = "";
    [Reactive] public string Password { get; set; } = "";
    
    public ReactiveCommand<Unit,Unit> TryLoginCommand { get; }
    public ReactiveCommand<Unit,Unit> TryRegisterCommand { get; }
    
    private static Task TryLogin() => Task.Delay(TimeSpan.FromSeconds(1));

    private static Task TryRegister() => Task.Delay(TimeSpan.FromSeconds(2));
    
    [RelayCommand]
    private void Nav()
    {
        var r = Navigation.To<ThirdTestViewModel>();
    }

    // [RelayCommand]
    // private void EnableBack()
    // {
    //     ((NavigationViewModel)Navigation).CanGoBackBool = true;
    // }
    //
    // [RelayCommand]
    // private void DisableBack()
    // {
    //     ((NavigationViewModel)Navigation).CanGoBackBool = false;
    // }
    //
    // [RelayCommand]
    // private void ToggleBack()
    // {
    //     ((NavigationViewModel)Navigation).CanGoBackBool ^= true;
    // }
}