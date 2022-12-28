using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using CommunityToolkit.Common;
using DDS.Core.Helper;
using DDS.Core.Services;
using Microsoft.VisualStudio.Threading;
using ReactiveUI.Validation.Extensions;

namespace DDS.Core.ViewModels;

[DataContract]
// [NotifyDataErrorInfoProperty]
public sealed partial class LoginViewModel : ViewModel
{
    private readonly ILoginService _loginService;
    public LoginViewModel() : this(Globals.Services) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public LoginViewModel(IServiceProvider services, ILoginService? loginService = null) : base(services)
    {
        // EnableAsyncInitPrepareActivate = false;
        // JoinInitBeforeOnActivationFinished = true;
        // JoinPrepareBeforeOnActivationFinished = true;
        // JoinActivationBeforeOnActivationFinished = true;

        _loginService = loginService ?? GetService<ILoginService>();

        IObservable<bool> canLogin = this.WhenAnyValue(
            x => x.LoginName, x => x.Password,
            (loginName, password)
                => !string.IsNullOrWhiteSpace(loginName) && !string.IsNullOrWhiteSpace(password));
        TryLoginCommand = ReactiveCommand.CreateFromTask(
            execute: TryLoginAsync,
            canExecute: canLogin);
        TryRegisterCommand = ReactiveCommand.CreateFromTask(
            execute: TryRegisterAsync,
            canExecute: canLogin);

        this.ValidationRule(x => x.Password, 
            text => !string.IsNullOrWhiteSpace(text),
            "You must specify a valid password.");
        
        this.ValidationRule(x => x.TestValidationReactiveProperty, 
            text => !string.IsNullOrWhiteSpace(text),
            "You must specify a valid value.");
        
        this.ValidationRule(x => x.TestValidationObservableProperty, 
            text => !string.IsNullOrWhiteSpace(text),
            "You must specify a valid value.");
        
        this.ValidationRule(x => x.LoginName, 
            text => !string.IsNullOrWhiteSpace(text) && (text.Contains('@') ? text.IsEmail() : Regex.IsMatch(text, "^[A-Za-z0-9_-]+$")),
            "You must specify a valid value.");
    }


    // protected override async Task InitializeAsync(CancellationToken cancellationToken)
    // {
    //     Console.WriteLine("s:Initialize Task");
    //     await Task.Yield();
    //     await Task.Delay(1000, cancellationToken);
    //     await Task.Delay(2000, cancellationToken);
    //     // throw new Exception("Evil Init");
    //     // await Task.Delay(4000);
    //     Console.WriteLine("_:Initialize Task done");
    // }
    //
    // protected override async Task OnPrepareAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    // {
    //     Console.WriteLine("s:Prepare Task");
    //     await Task.Yield();
    //     await Task.Delay(2000, cancellationToken);
    //     // throw new Exception("Evil prepare");
    //     Console.WriteLine("_:Prepare Task done");
    // }
    //
    // protected override async Task OnActivationAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    // {
    //     Console.WriteLine("s:Login Activation Task");
    //     await Task.Yield();
    //     await Task.Delay(2000, cancellationToken);
    //     // throw new Exception("Evil activation");
    //     Console.WriteLine("_:Login Activation Task done");
    //     if (_loginService.IsLoggedIn)
    //     {
    //         // Navigation.To<TestViewModel>();
    //     }
    //     // return Task.CompletedTask;
    // }

    protected override void OnActivation(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        Console.WriteLine("void OnActivation");
        ValidateAllProperties();

    }

    protected override void OnActivationFinishing(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        Console.WriteLine("void OnActivationFinishing");
    }
    
    //

    protected override void OnDeactivation()
    {
        
    }

    [Required]
    [MinLength(5)]
    [NotifyDataErrorInfoProperty]
    [Reactive] public string LoginName { get; set; } = "";
    [Reactive] public string Password { get; set; } = "";

    [MaxLength(2)]
    [NotifyDataErrorInfoProperty]
    [Reactive] public string TestValidationReactiveProperty { get; set; } = "";
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(2)]
    private string _testValidationObservableProperty = "";
    
    public ReactiveCommand<Unit,Unit> TryLoginCommand { get; }
    public ReactiveCommand<Unit,Unit> TryRegisterCommand { get; }
    
    private static Task TryLoginAsync() => Task.Delay(1.s());

    private static Task TryRegisterAsync() => Task.Delay(1.s());
}