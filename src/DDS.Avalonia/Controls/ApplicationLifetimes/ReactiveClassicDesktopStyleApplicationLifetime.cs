using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.VisualStudio.Threading;

namespace DDS.Avalonia.Controls.ApplicationLifetimes;

// todo evaluate moving abstractions of this to Core & potential LifetimeWrapper compatibility changes
// todo ReactiveSingleViewApplicationLifetime

/// <summary>
/// WIP.
/// Will be used for proper shutdown and canceling running tasks, e.g. for finishing essential DB write Tasks OnShutdown
/// </summary>
public class ReactiveClassicDesktopStyleApplicationLifetime : ClassicDesktopStyleApplicationLifetime
{
    public ReactiveClassicDesktopStyleApplicationLifetime(
        string[] args,
        ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose
        )
    {
        Args = args;
        ShutdownMode = shutdownMode;
    }

    public CancellationTokenSource? CancellationTokenSource { get; private set; }
    public CancellationToken CancellationToken { get; private set; }

    
    // todo put in core interface and fill somewhere
    public JoinableTask? CancelTask { get; set; }

    private readonly object _mutex = new();

    private volatile bool _isStarted;
    
    public async Task<(bool started, int exitCode)> StartAsync(
        string[] args, 
        CancellationTokenSource? cancellationTokenSource = null)
    {
        if (_isStarted) return (false, 0);
        lock (_mutex)
        {
            if (_isStarted) return (false, 0);
            _isStarted = true;
        }
        CancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
        CancellationToken = CancellationTokenSource.Token;
        int exitCode = Start(args); // mainLoop starts here and exits only - OnExit - (e.g. when closing last window)
        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = null;
        if (CancelTask is { } cancelTask) await cancelTask;
        _isStarted = false;
        return (true, exitCode);
    }
}