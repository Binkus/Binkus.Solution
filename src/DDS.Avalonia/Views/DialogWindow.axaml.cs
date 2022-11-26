using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDS.Avalonia.Views;

public partial class DialogWindow : BaseWindow<DialogViewModel>
{
    // public DialogWindow(WindowBase owner) : this() //=> Owner = owner;
    // {
    //     this.WhenActivated(disposable => Owner ??= owner);
    // }

    [UsedImplicitly]
    public DialogWindow()
    {
        InitializeComponent();
    }
}