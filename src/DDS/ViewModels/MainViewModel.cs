﻿using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DDS.Services;
using ReactiveUI.Fody.Helpers;

namespace DDS.ViewModels
{
    public partial class MainViewModel : ViewModelBase, IScreen
    {
        private readonly IAvaloniaEssentials _avaloniaEssentials;

        public string Greeting => "Greetings from MainView";

        [Reactive] public string GotPath { get; set; } = "fullPath is empty";

        public RoutingState Router { get; } = new();

        // Necessary for Designer: 
#pragma warning disable CS8618
        public MainViewModel() : this(default) { }
#pragma warning restore CS8618

        public MainViewModel(IAvaloniaEssentials? avaloniaEssentials)
        {
            _avaloniaEssentials ??= avaloniaEssentials ?? Globals.ServiceProvider.GetService<IAvaloniaEssentials>()!;
            
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

        [RelayCommand]
        async Task OpenFilePicker()
        {
            var fileResult = await _avaloniaEssentials.FilePickerAsync();
            var fullPath = fileResult.FullPath;
            GotPath = fileResult.Exists ? $"fullPath={fullPath}" : "fullPath is empty";
        }
    }
}