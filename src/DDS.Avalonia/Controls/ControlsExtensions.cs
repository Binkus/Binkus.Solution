using Avalonia;
using Avalonia.Platform;

namespace DDS.Avalonia.Controls;

public static class ControlsExtensions
{
    private const double Width = 400, Height = 150;
    
    private static void Center(DialogWindow dialog, Window topLevel)
    {
        
        
        // ~ PI * Thumb centering:
        double posX = topLevel.Position.X + topLevel.Width / 2 + Width / 2;
        double posY = topLevel.Position.Y + topLevel.Height / 2 - Height / 2;

        // posX = dialog.Bounds.X;
        // posY = dialog.Bounds.Y;
        
        posX = posX < 16 ? 16 : posX;
        posY = posY < 16 ? 16 : posY;
        dialog.Position = new PixelPoint((int)posX, (int)posY);
    }
    
    private static void CenterWindow(Window w)
    {
        if (w.WindowStartupLocation == WindowStartupLocation.Manual)
            return;
    
        Screen screen = w.Screens.ScreenFromVisual(w) ?? throw new NullReferenceException();
        // Screen? screen = null;
        // while (screen == null) {
        //     await Task.Delay(1);
        //     screen = w.Screens.ScreenFromVisual(w);
        // }
    
        if (w.WindowStartupLocation == WindowStartupLocation.CenterScreen) {
            // ReSharper disable once PossibleLossOfFraction
            var x = (int)Math.Floor(screen.Bounds.Width / 2 - w.Bounds.Width / 2);
            // ReSharper disable once PossibleLossOfFraction
            var y = (int)Math.Floor(screen.Bounds.Height / 2 - (w.Bounds.Height + 30) / 2);
    
            w.Position = new PixelPoint(x, y);
        }
        else if (w.WindowStartupLocation == WindowStartupLocation.CenterOwner)
        {
            if (w.Owner is not Window pw) return;
            var x = (int)Math.Floor(pw.Bounds.Width / 2 - w.Bounds.Width / 2 + pw.Position.X);
            var y = (int)Math.Floor(pw.Bounds.Height / 2 - (w.Bounds.Height + 30) / 2 + pw.Position.Y);
    
            w.Position = new PixelPoint(x, y);
        }
    }
    
    private static void SetWindowStartupLocationWorkaround(Window w) {
        // if(OperatingSystem.IsWindows()) { // Not needed for Windows
        //     return;
        // }
        
        // Window w = null!;
        
        
        
        //
        double scale = w.PlatformImpl?.DesktopScaling ?? 1.0;
        IWindowBaseImpl? ownerPlatformImpl = w.Owner?.PlatformImpl;
        if(ownerPlatformImpl != null) {
            scale = ownerPlatformImpl.DesktopScaling;
        }
        PixelRect rect = new PixelRect(PixelPoint.Origin,
            PixelSize.FromSize(w.ClientSize, scale));
        if(w.WindowStartupLocation == WindowStartupLocation.CenterScreen) {
            Screen? screen = w.Screens.ScreenFromPoint(ownerPlatformImpl?.Position ?? w.Position);
            if(screen == null) {
                return;
            }
            w.Position = screen.WorkingArea.CenterRect(rect).Position;
        }
        else {
            if(ownerPlatformImpl == null ||
               w.WindowStartupLocation != WindowStartupLocation.CenterOwner) {
                return;
            }
            w.Position = new PixelRect(ownerPlatformImpl.Position,
                PixelSize.FromSize(ownerPlatformImpl.ClientSize, scale)).CenterRect(rect).Position;
        }
    }
}