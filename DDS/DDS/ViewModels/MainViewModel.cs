using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI.Fody.Helpers;

namespace DDS.ViewModels
{
    public partial class MainViewModel : ViewModelBase, IScreen
    {
        public string Greeting => "Greetings from MainView";

        public RoutingState Router { get; } = new();

        public MainViewModel()
        {
            HostScreen = this;
            
            GoTest = ReactiveCommand.CreateFromObservable(
                () => Router.NavigateAndReset.Execute(new TestViewModel() { HostScreen = this }),
                canExecute: this.WhenAnyObservable(x => x.Router.CurrentViewModel).Select(x => x is not TestViewModel)
            );
            GoSecondTest = ReactiveCommand.CreateFromObservable(
                () => Router.NavigateAndReset.Execute(new SecondTestViewModel() { HostScreen = this }),
                canExecute: this.WhenAnyObservable(x => x.Router.CurrentViewModel).Select(x => x is not SecondTestViewModel)
            );
            
            var canGoBack = this
                .WhenAnyValue(x => x.Router.NavigationStack.Count)
                .Select(count => count > 0);
            GoBack = ReactiveCommand.CreateFromObservable(
                () => Router.NavigateBack.Execute(Unit.Default),
                canGoBack);
        }
        
        public ReactiveCommand<Unit, IRoutableViewModel?> GoBack { get; } 
        public ReactiveCommand<Unit, IRoutableViewModel> GoTest { get; }
        public ReactiveCommand<Unit, IRoutableViewModel> GoSecondTest { get; }
    }
}