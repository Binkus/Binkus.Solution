using System.Reactive;
using Avalonia.Controls;
using DDS.Controls;
using DDS.ViewModels;

namespace DDS.Views;

public sealed partial class MainView : BaseUserControl<MainViewModel>
{
    // empty ctor for Designer
    public MainView() : this(Globals.GetService<IViewLocator>()) { }

    [ActivatorUtilitiesConstructor, UsedImplicitly]
    public MainView(IViewLocator viewLocator)
    {
        InitializeComponent();
        // Optional, but gets resolved anyway but by default, but for each navigation, with this change only once:
        RoutedViewHost.ViewLocator = viewLocator;
        
        this.WhenActivated((CompositeDisposable d) =>
        {
            var topLevel = GetTopLevel();
            
            // Disposable.Create();
        });

        // this.WhenActivated((CompositeDisposable d) =>
        // {
        //     try
        //     {
        //         SecondTestView.MainViewTopLevel = GetTopLevel();
        //     }
        //     catch (Exception e)
        //     {
        //         //
        //     }    
        // });
        
        
    }
}