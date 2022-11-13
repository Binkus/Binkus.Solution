using Avalonia.Controls;
using DDS.Avalonia.Controls;
using DDS.Avalonia.ViewModels;

namespace DDS.Avalonia.Views;

[UsedImplicitly]
public sealed partial class MainWindow : BaseWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}