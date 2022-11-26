using Avalonia.Threading;
using DDS.Core;
using DDS.Core.Helper;
using DDS.Core.Services;

namespace DDS.Avalonia.Helper;

public static class WindowSpawnHelper
{
    // Spawn another MainWindow for scope testing
    public static void SpawnMainWindow(bool spawn)
    {
        if (spawn) 
            Task.Run(async () =>
            {
                await 4.Seconds();
                    
                Dispatcher.UIThread.Post(() =>
                {
                    var window2 = Globals.GetService<ServiceScopeManager>().CreateScope()
                        .ServiceProvider.GetRequiredService<MainWindow>();
                    window2.Show();
                
                    window2.Height = 960;
                    window2.Width = 690;
                }, DispatcherPriority.Background);
            });
    }
}