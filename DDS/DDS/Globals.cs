using System.Diagnostics;
using JetBrains.Annotations;

namespace DDS;

public interface Globals
{
    interface ISetGlobals
    {
        // static IAppCore SetInstanceNullable(IAppCore value) => _instanceNullable = value;
        // static bool SetIsDesignMode(bool value) => IsDesignMode = value;
        // static IServiceProvider SetServiceProvider(IServiceProvider value) => ServiceProvider = value;
        // static object SetApplicationLifetime(object value) => ApplicationLifetime = value;
        // static bool SetIsClassicDesktopStyleApplicationLifetime(bool value) => IsClassicDesktopStyleApplicationLifetime = value;
        
        [UsedImplicitly] static App? InstanceNullable { get => _instanceNullable; set => _instanceNullable = value; }
        [UsedImplicitly] static bool IsDesignMode { get => Globals.IsDesignMode; set => Globals.IsDesignMode = value; }
        [UsedImplicitly] static IServiceProvider ServiceProvider { get => Globals.ServiceProvider; set => Globals.ServiceProvider = value; }
        [UsedImplicitly] static object ApplicationLifetime { get => Globals.ApplicationLifetime; set => Globals.ApplicationLifetime = value; }

        [UsedImplicitly]
        static bool IsClassicDesktopStyleApplicationLifetime
        {
            get => Globals.IsClassicDesktopStyleApplicationLifetime; set => Globals.IsClassicDesktopStyleApplicationLifetime = value;
        }
    }
    
    #region Static Globals

    static readonly Stopwatch Stopwatch = new();
    static Task InitStartup = null!;
    static Task DbMigrationTask = null!;
    
    private static App? _instanceNullable;
    [UsedImplicitly] static App Instance 
        => _instanceNullable ?? throw new NullReferenceException($"{nameof(_instanceNullable)} is null");
    
    [UsedImplicitly] static bool IsDesignMode { get; private set; }
    [UsedImplicitly] static IServiceProvider ServiceProvider { get; private set; } = null!;
    [UsedImplicitly] static object ApplicationLifetime { get; private set; } = null!;
    [UsedImplicitly] static bool IsClassicDesktopStyleApplicationLifetime { get; private set; }

    #endregion
}