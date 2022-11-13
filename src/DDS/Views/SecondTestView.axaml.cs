using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DDS.Views;

[UsedImplicitly]
public sealed partial class SecondTestView : BaseUserControl<SecondTestViewModel>
{
    // public static TopLevel? MainViewTopLevel;
    // public static TopLevel? SecondTestViewTopLevel;
    
    public SecondTestView()
    {
        InitializeComponent();
        
        // this.WhenActivated((CompositeDisposable d) =>
        // {
        //     var asdf = 1;
        //     try
        //     {
        //         SecondTestViewTopLevel = GetTopLevel();
        //     }
        //     catch (Exception e)
        //     {
        //         //
        //     }
        //
        //     var r0 = MainViewTopLevel == SecondTestViewTopLevel;
        //     var r1 = ReferenceEquals(MainViewTopLevel, SecondTestViewTopLevel);
        //     var r2 = MainViewTopLevel?.Equals(SecondTestViewTopLevel);
        //     var r3 = Equals(MainViewTopLevel, SecondTestViewTopLevel);
        //     
        //     
        //
        //     Console.WriteLine();
        // });
    }
}