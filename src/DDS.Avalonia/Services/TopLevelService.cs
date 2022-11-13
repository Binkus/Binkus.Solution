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
}