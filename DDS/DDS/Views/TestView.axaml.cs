using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDS.Views;

public partial class TestView : BaseUserControl<TestViewModel>
{
    public TestView()
    {
        InitializeComponent();
    }

    // private void InitializeComponent()
    // {
    //     AvaloniaXamlLoader.Load(this);
    // }
}