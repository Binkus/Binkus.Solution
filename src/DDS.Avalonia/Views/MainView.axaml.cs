using System.Reactive;
using Avalonia.Controls;
using DDS.Avalonia.Controls;
using DDS.Core;
using DDS.Core.ViewModels;

namespace DDS.Avalonia.Views;

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
    }
}