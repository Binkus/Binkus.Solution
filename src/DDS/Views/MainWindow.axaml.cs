using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using DDS.Controls;
using DDS.ViewModels;

namespace DDS.Views
{
    public partial class MainWindow : BaseWindow<MainWindowViewModel>
    {
        private bool _done;

        public MainWindow()
        {
            InitializeComponent();
            
            // var iv = this.GetObservable(Window.IsVisibleProperty);
            // iv.Subscribe(value =>
            // {
            //     if (value && !_done) {
            //         _done = true;
            //         // CenterWindow();
            //         SetWindowStartupLocationWorkaround();
            //
            //     }
            // });
            //
            // SetWindowStartupLocationWorkaround();
        }
        
        // public override async void Show()
        // {
        //     base.Show();
        //     await Task.Delay(1);
        //     SetWindowStartupLocationWorkaround();
        // }
        //
        // private void SetWindowStartupLocationWorkaround() {
        //     if(OperatingSystem.IsWindows()) { // Not needed for Windows
        //         return;
        //     }
        //
        //     double scale = PlatformImpl?.DesktopScaling ?? 1.0;
        //     IWindowBaseImpl? powner = Owner?.PlatformImpl;
        //     if(powner != null) {
        //         scale = powner.DesktopScaling;
        //     }
        //     PixelRect rect = new PixelRect(PixelPoint.Origin,
        //         PixelSize.FromSize(ClientSize, scale));
        //     if(WindowStartupLocation == WindowStartupLocation.CenterScreen) {
        //         Screen? screen = Screens.ScreenFromPoint(powner?.Position ?? Position);
        //         if(screen == null) {
        //             return;
        //         }
        //         Position = screen.WorkingArea.CenterRect(rect).Position;
        //     }
        //     else {
        //         if(powner == null ||
        //            WindowStartupLocation != WindowStartupLocation.CenterOwner) {
        //             return;
        //         }
        //         Position = new PixelRect(powner.Position,
        //             PixelSize.FromSize(powner.ClientSize, scale)).CenterRect(rect).Position;
        //     }
        // }
        //
        //
        // private async void CenterWindow()
        // {
        //     if (this.WindowStartupLocation == WindowStartupLocation.Manual)
        //         return;
        //
        //     Screen? screen = null;
        //     while (screen == null) {
        //         await Task.Delay(1);
        //         screen = this.Screens.ScreenFromVisual(this);
        //     }
        //
        //     if (this.WindowStartupLocation == WindowStartupLocation.CenterScreen) {
        //         // ReSharper disable once PossibleLossOfFraction
        //         var x = (int)Math.Floor(screen.Bounds.Width / 2 - this.Bounds.Width / 2);
        //         // ReSharper disable once PossibleLossOfFraction
        //         var y = (int)Math.Floor(screen.Bounds.Height / 2 - (this.Bounds.Height + 30) / 2);
        //
        //         this.Position = new PixelPoint(x, y);
        //     } else if (this.WindowStartupLocation == WindowStartupLocation.CenterOwner) {
        //         if (this.Owner is Window pw) {
        //             var x = (int)Math.Floor(pw.Bounds.Width / 2 - this.Bounds.Width / 2 + pw.Position.X);
        //             var y = (int)Math.Floor(pw.Bounds.Height / 2 - (this.Bounds.Height + 30) / 2 + pw.Position.Y);
        //
        //             this.Position = new PixelPoint(x, y);
        //         }
        //     }
        // }
    }
}