using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using DDS.Controls;
using DDS.ViewModels;

namespace DDS.Views;

[UsedImplicitly]
public sealed partial class MainWindow : BaseWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}