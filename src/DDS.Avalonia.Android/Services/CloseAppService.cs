using DDS.Avalonia.Services;
using DDS.Core.Services;

namespace DDS.Avalonia.Android.Services;

public class CloseAppService : ICloseAppService
{
    public Action? CleanupAction { get; set; } = null;

    public void CloseApp(Action? cleanupAction = default)
    {
        cleanupAction?.Invoke();
        CleanupAction?.Invoke();

        var activity = MainActivity.CurrentMainActivity;
        MainActivity.CurrentMainActivity = null;

        try
        {
            activity?.FinishAffinity();
        }
        catch (Exception)
        {
            //
        }
        try
        {
            activity?.Finish();
        }
        catch (Exception)
        {
            //
        }
        try
        {
            System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
        }
        catch (Exception)
        {
            //
        }
        try
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        catch (Exception)
        {
            //
        }
    }
}