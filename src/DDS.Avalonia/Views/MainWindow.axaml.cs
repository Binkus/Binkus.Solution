using Avalonia.Controls;
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