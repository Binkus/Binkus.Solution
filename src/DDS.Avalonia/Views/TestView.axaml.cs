using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DDS.Core.ViewModels;

namespace DDS.Avalonia.Views;

[UsedImplicitly]
public sealed partial class TestView : BaseUserControl<TestViewModel>
{
    public TestView()
    {
        InitializeComponent();
    }
}