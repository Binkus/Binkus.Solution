using Avalonia.Controls.ApplicationLifetimes;
using DDS.Core.Controls;

namespace DDS.Avalonia.Controls;

public sealed class DesktopLifetimeWrapper : LifetimeWrapper, IClassicDesktopStyleApplicationLifetime
{
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

    public DesktopLifetimeWrapper(IClassicDesktopStyleApplicationLifetime lifetime) : base(lifetime)
    {
        _lifetime = lifetime;
    }
    
    public void Shutdown(int exitCode = 0)
    {
        _lifetime.Shutdown(exitCode);
    }

    public event EventHandler<ControlledApplicationLifetimeStartupEventArgs>? Startup
    {
        add => _lifetime.Startup += value;
        remove => _lifetime.Startup -= value;
    }

    public event EventHandler<ControlledApplicationLifetimeExitEventArgs>? Exit
    {
        add => _lifetime.Exit += value;
        remove => _lifetime.Exit -= value;
    }

    public bool TryShutdown(int exitCode = 0)
    {
        return _lifetime.TryShutdown(exitCode);
    }

    public string[]? Args => _lifetime.Args;

    public ShutdownMode ShutdownMode
    {
        get => _lifetime.ShutdownMode;
        set => _lifetime.ShutdownMode = value;
    }

    public Window? MainWindow
    {
        get => _lifetime.MainWindow;
        set => _lifetime.MainWindow = value;
    }

    public IReadOnlyList<Window> Windows => _lifetime.Windows;

    public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested
    {
        add => _lifetime.ShutdownRequested += value;
        remove => _lifetime.ShutdownRequested -= value;
    }
}

public sealed class SingleViewLifetimeWrapper : LifetimeWrapper, ISingleViewApplicationLifetime
{
    private readonly ISingleViewApplicationLifetime _lifetime;

    public SingleViewLifetimeWrapper(ISingleViewApplicationLifetime lifetime) : base(lifetime)
    {
        _lifetime = lifetime;
    }

    public Control? MainView
    {
        get => _lifetime.MainView;
        set => _lifetime.MainView = value;
    }
}

public abstract class LifetimeWrapper : ICoreLifetime
{
    private readonly IApplicationLifetime _lifetime;

    protected LifetimeWrapper(IApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }
}