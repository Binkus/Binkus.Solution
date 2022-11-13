using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DDS.Core.ViewModels;

namespace DDS.Avalonia.Views;

[UsedImplicitly]
public sealed partial class SecondTestView : BaseUserControl<SecondTestViewModel>
{
    public SecondTestView()
    {
        InitializeComponent();
    }
}