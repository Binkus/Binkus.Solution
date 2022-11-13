using DDS.Services;

namespace DDS.Desktop.Services;

public class CloseAppService : ICloseAppService
{
    public Action? CleanupAction { get; set; } = null;

    public void CloseApp(Action? cleanupAction = default)
    {
        cleanupAction?.Invoke();
        CleanupAction?.Invoke();

        try
        {
            Environment.Exit(0);
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