namespace DDS.Services;

public interface ICloseAppService
{
    Action? CleanupAction { get; set; }
    
    void CloseApp(Action? cleanupAction = default);
}