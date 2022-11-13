using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDS.Avalonia.Views;

[UsedImplicitly]
public sealed partial class TestView : BaseUserControl<TestViewModel>
{
    public TestView()
    {
        InitializeComponent();
    }
}