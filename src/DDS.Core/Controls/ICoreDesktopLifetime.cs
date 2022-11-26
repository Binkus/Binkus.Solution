namespace DDS.Core.Controls;

public interface ICoreDesktopLifetime : ICoreLifetime
{
    /// <summary>
    /// Shuts down the application and sets the exit code that is returned to the operating system when the application exits.
    /// </summary>
    /// <param name="exitCode">An integer exit code for an application. The default exit code is 0.</param>
    void Shutdown(int exitCode = 0);
}