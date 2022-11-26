using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDS.Avalonia.Views;

public partial class ThirdTestView : BaseUserControl<ThirdTestViewModel>
{
    public ThirdTestView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}