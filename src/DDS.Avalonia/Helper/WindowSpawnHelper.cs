using Avalonia.Threading;
using DDS.Core;
using DDS.Core.Helper;
using DDS.Core.Services;
using DynamicData.Kernel;

namespace DDS.Avalonia.Helper;

public static class WindowSpawnHelper
{
    // Spawn another MainWindow for scope testing
    public static void SpawnMainWindow()
    {
        
        // Task.Run(async () =>
        // {
        //     await 4.Seconds();
        //         
        //     // RxApp.MainThreadScheduler.Schedule(null, (_, _)=>
        //     
        //     Dispatcher.UIThread.Post(() =>
        //     {
        //         var window2 = Globals.GetService<ServiceScopeManager>().CreateScope()
        //             .ServiceProvider.GetRequiredService<MainWindow>();
        //         window2.Show();
        //     
        //         window2.Height = 960;
        //         window2.Width = 690;
        //     }, DispatcherPriority.Background);
        // });
        
        RxApp.TaskpoolScheduler.Schedule(SpawnWindow, TimeSpan.FromSeconds(4), (_, state)
            => Observable.Start(state, RxApp.MainThreadScheduler).Subscribe());

        // RxApp.MainThreadScheduler.ScheduleRecurringAction(15.Seconds(), SpawnWindow);
    }

    private static void SpawnWindow()
    {
        var window2 = Globals.GetService<ServiceScopeManager>().CreateScope()
            .ServiceProvider.GetRequiredService<MainWindow>();
        window2.Show();
                
        window2.Height = 960;
        window2.Width = 690;
    }
}