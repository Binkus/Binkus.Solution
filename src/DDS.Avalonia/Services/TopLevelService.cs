using DDS.Core;

namespace DDS.Avalonia.Services;

public class TopLevelService
{
    private readonly IServiceProvider _services;
    public TopLevelService(IServiceProvider services) => _services = services;

    private const string ExceptionMessage = "is not registered, maybe change some Singletons to Scoped; " +
                                            "fallbacking to main scope";
    private const string TopLevelExceptionMessage = $"TopLevel {ExceptionMessage}";
    private const string TopLevelWindowExceptionMessage = $"TopLevel Window {ExceptionMessage}";
    private const string FailedMessage = " failed.";
    
    public async Task<TopLevel> CurrentTopLevel()
    {
        int count = 0;
        while(_currentTopLevel is null)
        {
            await Task.Yield();
            
            if (++count == 42)
            {
                var tls = _services.GetRequiredService<TopLevelService>();
                if (tls != _services.GetRequiredService<TopLevelService>() || tls == this)
                    throw new InvalidOperationException(TopLevelExceptionMessage + FailedMessage);
                await Console.Error.WriteLineAsync(TopLevelExceptionMessage);
                return await tls.CurrentTopLevel();
            }
            
            if (count == 10) await Task.Delay(16);
        }

        return _currentTopLevel;
    }

    private TopLevel? _currentTopLevel;

    public TopLevel SetCurrentTopLevel { set => _currentTopLevel = value; }
    
    //
    
    public async Task<Window> CurrentWindow()
    {
        if (Globals.IsClassicDesktopStyleApplicationLifetime is false)
        {
            throw new NotSupportedException();
        }
        
        int count = 0;
        while(_currentWindow is null)
        {
            await Task.Yield();
            
            if (++count == 42)
            {
                var tls = _services.GetRequiredService<TopLevelService>();
                if (tls != _services.GetRequiredService<TopLevelService>() || tls == this)
                    throw new InvalidOperationException(TopLevelWindowExceptionMessage + FailedMessage);
                await Console.Error.WriteLineAsync(TopLevelWindowExceptionMessage);
                return await tls.CurrentWindow();
            }
            
            if (count == 10) await Task.Delay(16);
        }

        return _currentWindow;
    }

    private Window? _currentWindow;

    public Window? SetCurrentWindow { set => _currentWindow = value; }
}