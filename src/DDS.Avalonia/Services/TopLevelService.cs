using DDS.Core;

namespace DDS.Avalonia.Services;

public class TopLevelService
{
    public async Task<TopLevel> CurrentTopLevel()
    {
        while(_currentTopLevel is null)
        {
            await Task.Yield();
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
        
        while(_currentWindow is null)
        {
            await Task.Yield();
        }

        return _currentWindow;
    }

    private Window? _currentWindow;

    public Window? SetCurrentWindow { set => _currentWindow = value; }
}