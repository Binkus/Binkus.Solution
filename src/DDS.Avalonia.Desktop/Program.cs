using System.Diagnostics;
using Avalonia;
using DDS.Avalonia.Desktop.Controls;
using DDS.Avalonia.Desktop.Services;
using DDS.Avalonia.Services;
using DDS.Core.Controls;
using DDS.Core.Helper;
using DDS.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using TaskExtensions = DDS.Core.Helper.TaskExtensions;

namespace DDS.Avalonia.Desktop
{
    internal static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .DoSomeTests().GetAwaiter().GetResult()
                .ConfigureAppServices(services => services
                        .AddSingleton<ICloseAppService,CloseAppService>()
                        .AddScoped<IDialogAlertMessageBox,DialogAlertMessageBox>()
                    )
                .UsePlatformDetect()
                .LogToTrace();

        private static async ValueTask<AppBuilder> DoSomeTests(this AppBuilder appBuilder)
        {
            // var t1 = Task.Run(async() =>
            // {
            //     await Task.Delay(1000);
            //     Console.WriteLine("1s");
            //     return "";
            // });
            // var t2 = Task.Run(async () =>
            // {
            //     await Task.Delay(2000);
            //     Console.WriteLine("2s Exception");
            //     throw new EndOfStreamException();
            //     return "";
            // });
            // var t3 = Task.Run(async() =>
            // {
            //     await Task.Delay(9000);
            //     Console.WriteLine("9s");
            //     return "";
            // });
            //
            // await TaskExtensions.WhenAll(t1, t2, t3);
            
            //

            // var process = new Process();
            // // Configure the process using the StartInfo properties.
            // process.StartInfo.FileName = "konsole";
            // // process.StartInfo.Arguments = "-n";
            // process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            // process.Start();
            //
            // // var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(0));
            // // var processTask = process.ToCancelableTask(tokenSource.Token);
            //
            // var r = await Task.Run(async () =>
            // {
            //     // await Task.Delay(10000);
            //     return await process;
            // });
            //
            //
            //
            return appBuilder;
        }
    }
}