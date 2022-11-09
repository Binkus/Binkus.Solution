using System.Reactive;
using Avalonia.Controls;
using DDS.Controls;
using DDS.ViewModels;

namespace DDS.Views
{
    public sealed partial class MainView : BaseUserControl<MainViewModel>
    {
        // empty ctor for Designer
        public MainView() : this(Globals.ServiceProvider, Globals.ServiceProvider.GetRequiredService<IViewLocator>()) { }

        // ReSharper disable once MemberCanBePrivate.Global
        [ActivatorUtilitiesConstructor]
        public MainView(IServiceProvider services, IViewLocator viewLocator)
        {
            InitializeComponent();
            // Optional, but gets resolved anyway but by default, but for each navigation, with this change only once:
            RoutedViewHost.ViewLocator = viewLocator;
            Services = services;
        }
    }
}