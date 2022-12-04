using DDS.Core.Helper;
using DDS.Core.Services;
using Microsoft.VisualStudio.Threading;

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


    protected override async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("s:Initialize Task");
        await Task.Yield();
        await Task.Delay(1000, cancellationToken);
        await Task.Delay(2000, cancellationToken);
        // await Task.Delay(4000);
        Console.WriteLine("_:Initialize Task done");
    }
    
    protected override async Task OnPrepareAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        Console.WriteLine("s:Prepare Task");
        await Task.Yield();
        await Task.Delay(2000, cancellationToken);
        // throw new Exception("Evil prepare");
        Console.WriteLine("_:Prepare Task done");
    }
    
    protected override async Task OnActivationAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        Console.WriteLine("s:Login Activation Task");
        await Task.Yield();
        await Task.Delay(2000, cancellationToken);
        // throw new Exception("Evil activation");
        Console.WriteLine("_:Login Activation Task done");
        if (_loginService.IsLoggedIn)
        {
            // Navigation.To<TestViewModel>();
            // Nav2();
        }
    
        // return Task.CompletedTask;
    }
    
    protected override void OnActivationFinished(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        Console.WriteLine("void HandleActivation");
    }
    
    //

    protected override void OnDeactivation()
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
    
    private async void Nav2()
    {
        await 4.Seconds();
        Navigation.To<SecondTestViewModel>();
    }
    
    [RelayCommand]
    private void Test()
    {
        Console.WriteLine($"Test:Before potential deadlock | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
        // var t = TestAsync();
        // t.Start(); // Start may not be called on a promise-style task.
        // t.Wait(); // deadlock 
        var jtf = new JoinableTaskFactory(new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current));
        var jt = jtf.RunAsync(
            async () =>
            {
                // Avoid awaiting or returning a Task representing work that was not started within your context as that
                // can lead to deadlocks. Start the work within this context, or use JoinableTaskFactory.
                // RunAsync to start the task and await the returned JoinableTask instead.
                Console.WriteLine($"1: jtf before await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
                // await t;
                await TestAsync(1,5);
                Console.WriteLine($"1: jtf after await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");

                // Console.WriteLine(" | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
            });
        // var jt2 = jt.Task.ContinueWith(_ => jtf.RunAsync(
        //     async () =>
        //     {
        //         // Avoid awaiting or returning a Task representing work that was not started within your context as that
        //         // can lead to deadlocks. Start the work within this context, or use JoinableTaskFactory.
        //         // RunAsync to start the task and await the returned JoinableTask instead.
        //         Console.WriteLine($"jtf 2 before await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
        //         // await t;
        //         await TestAsync(2);
        //         Console.WriteLine($"jtf 2 after await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
        //
        //         // Console.WriteLine(" | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
        //     }));
        // jt2.GetAwaiter().GetResult().Join();

        var jt2 = jtf.RunAsync(
            async () =>
            {
                // Avoid awaiting or returning a Task representing work that was not started within your context as that
                // can lead to deadlocks. Start the work within this context, or use JoinableTaskFactory.
                // RunAsync to start the task and await the returned JoinableTask instead.
                Console.WriteLine($"2: jtf before await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
                // await t;
                await TestAsync(2,2);
                // throw new Exception("Ex");
                Console.WriteLine($"2: jtf after await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");

                // Console.WriteLine(" | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
            });
        
        
        var jt3 = jtf.RunAsync(
            async () =>
            {
                var j1 = jt.JoinAsync();
                var j2 = jt2.JoinAsync();
                await j1;
                await j2;
            });

        Console.WriteLine("\nbefore join\n");
        // jt.Join();
        // Console.WriteLine($"1:join_done");
        // jt2.Join();
        // Console.WriteLine($"2:join_done");
        jt3.Join();
        Console.WriteLine($"Test:After potential deadlock | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine("#########################################\n");

        var gL = () =>
        {

            var s = Services.CreateScope();
            var r = s.ServiceProvider.GetRequiredService<LoginViewModel>();
            s.Dispose();
            return r;
        };

        var l0 = gL();
        var l1 = gL();
        var l2 = gL();
    }

    private async Task TestAsync(int i = 0, int s = 5)
    {
        Console.WriteLine($"{i}:s={s}:TestAsync:Before Await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
        await s.Seconds();
        // await 5.Seconds().ConfigureAwait(false);
        // await Task.Delay(1).ConfigureAwait(false);
        Console.WriteLine($"{i}:s={s}:TestAsync:After Await | Current Thread:{Thread.CurrentThread.ManagedThreadId}");
    }

    [RelayCommand]
    private Task Test2Async()
    {
        var f = new JoinableTaskFactory(new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current));
        // new JoinableTaskFactory(new JoinableTaskCollection());
        var t = TestAsync();
        
        // f.RunAsync(() =>
        // {
        //     // return t;
        // }).Join();
        return Task.CompletedTask;
    }
    
    [RelayCommand]
    private async Task Test3Async()
    {
        // Task[] tasks = new Task[2];
        // tasks[0] = Dummy0Async();
        // tasks[1] = Dummy1Async();
        //
        // try
        // {
        //     await tasks;
        // }
        // catch (AggregateException e)
        // {
        //     Console.WriteLine(e);
        //     throw;
        // }

        // IsActivated = false;




    }

    async Task Dummy0Async()
    {
        await 5.s();
        throw new ArgumentException(0 + ":Dummy");
    }
    
    async Task Dummy1Async()
    {
        await 10.s();
        Console.WriteLine("1 dummy done");
        throw new InvalidOperationException(1 + ":Dummy");
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