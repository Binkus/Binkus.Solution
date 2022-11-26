using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using DDS.Core.Helper;
using ReactiveMarbles.ObservableEvents;

namespace DDS.Avalonia.Views;

public partial class LoginView : BaseUserControl<LoginViewModel>
{
    public LoginView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // this.WhenAnyObservable(x => x.ViewModel.TryLoginCommand.IsExecuting)
            //     .BindTo(this, x => x.LoginProgressBar.IsVisible)
            //     .DisposeWith(disposables);
            // this.WhenAnyObservable(x => x.ViewModel.TryRegisterCommand.IsExecuting)
            //     .BindTo(this, x => x.LoginProgressBar.IsVisible)
            //     .DisposeWith(disposables);
            ObserveProgressbar(disposables,x => x.ViewModel.TryLoginCommand.IsExecuting);
            ObserveProgressbar(disposables,x => x.ViewModel.TryRegisterCommand.IsExecuting);
        
        
            this.Events().KeyDown
                .Where(args => args.Key == Key.Escape)
                .Subscribe(args => ViewModel.Password = "")
                .DisposeWith(disposables);
        
            this.Events().DoubleTapped
                .Where(args => args.Source?.Equals(LoginProgressBar) ?? false)
                .Subscribe(args => ViewModel.Password = "")
                .DisposeWith(disposables);
            
            this.Events().KeyDown
                .Where(args => args.Key == Key.Enter)
                .Subscribe(args =>
                {
                    if (((ICommand)ViewModel.TryLoginCommand).CanExecute(null) is false) return;
                    // ViewModel.TryLoginCommand.Execute(Unit.Default).Subscribe(); // execs the underlying cmd
                    // ViewModel.TryLoginCommand.Execute(Unit.Default); // execs the underlying cmd
                    // ViewModel.Navigation.Router.Navigate.Execute(new TestViewModel());
                    
                    // var d = ViewModel.TryLoginCommand.Execute(Unit.Default).Subscribe();
                    //
                    // // fix for command, executing without subscription does not notify IsExecuting properly, the
                    // // following will then dispose the subscription after IsExecuting got notified, disposing
                    // // subscription directly will NOT notify IsExecuting, so e.g. resulting in progressbar not showing,
                    // // (but still everything executes the actual cmd), disposing the WhenAnyObservable subscription
                    // // directly seems to be not a problem at all and works as expected.
                    // // The disposables - both, have to be disposed, not with the CompositeDisposables disposables variable
                    // // but directly after the execution is done, or potential memory leak (until view deactivated).
                    // // Btw it is not executing the subscription action when disposed immediately.
                    // this.WhenAnyObservable(x => x.ViewModel.TryLoginCommand.IsExecuting)
                    //     .Subscribe(x => d.Dispose())
                    //     .Dispose();

                    
                    // // Shorter black magic alternative for self disposing
                    // IDisposable? disposable = null;
                    // // ReSharper disable once AccessToModifiedClosure
                    // disposable = ViewModel.TryLoginCommand.Execute(Unit.Default).Subscribe(x => disposable?.Dispose());
                    ViewModel.TryLoginCommand.Execute(Unit.Default).SubscribeAndDisposeOnNext();
                })
                .DisposeWith(disposables);

            // var a = this.WhenAnyObservable(x => x.ViewModel.TryLoginCommand.CanExecute).Subscribe();
        });
    }
    
    private void ObserveProgressbar(CompositeDisposable disposables, Expression<Func<LoginView,IObservable<bool>?>> e) =>
        this.WhenAnyObservable(e)
            .BindTo(this, x => x.LoginProgressBar.IsVisible)
            .DisposeWith(disposables);
}