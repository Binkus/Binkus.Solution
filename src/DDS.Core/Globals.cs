using DDS.Core.Controls;

namespace DDS.Core;

public static class Globals
{
    private static readonly object Locke = new();

    private static volatile bool _startupDone;

    private static T D<T>(this T t)
    {
        lock (Locke) return _startupDone 
                ? throw new InvalidOperationException("Startup is already done, Globals are immutable") 
                : t;
    }

    [UsedImplicitly] public static bool IsStartupDone { get { lock (Locke) return _startupDone; } }

    public interface ISetGlobalsOnlyOnceOnStartup
    {
        [UsedImplicitly] static IAppCore? InstanceNullable { get => _instanceNullable; set => _instanceNullable = D(value); }
        [UsedImplicitly] static bool IsDesignMode { private get => Globals.IsDesignMode; set => Globals.IsDesignMode = value.D(); }
        [UsedImplicitly] static IServiceProvider ServiceProvider { private get => Globals.Services; set => Globals.Services = value.D(); }
        [UsedImplicitly] static object ApplicationLifetime { private get => Globals.ApplicationLifetime; set => Globals.ApplicationLifetime = value.D(); }
        [UsedImplicitly] static ICoreLifetime ApplicationLifetimeWrapped { private get => Globals.ApplicationLifetimeWrapped; set => Globals.ApplicationLifetimeWrapped = value.D(); }

        [UsedImplicitly]
        static bool IsClassicDesktopStyleApplicationLifetime
        {
            private get => Globals.IsClassicDesktopStyleApplicationLifetime; set => Globals.IsClassicDesktopStyleApplicationLifetime = value.D();
        }
        
        [UsedImplicitly] static Task DbMigrationTask  { private get => Globals.DbMigrationTask; set => Globals.DbMigrationTask = value.D(); }

        [UsedImplicitly] static void FinishGlobalsSetupByMakingGlobalsImmutable()
        {
            if (ServiceProvider is null || (!IsDesignMode && (ApplicationLifetime is null || ApplicationLifetimeWrapped is null)) || _instanceNullable is null || DbMigrationTask is null)
            {
                throw new NullReferenceException("Globals setup has not been done correctly");
            }
            lock (Locke) _startupDone = true.D();
        }
    }
    
    #region Static Globals

    // public static readonly Stopwatch Stopwatch = new();
    // public static Task InitStartup = null!;
    [UsedImplicitly] public static Task DbMigrationTask { get; private set; } = null!;
    
    private static IAppCore? _instanceNullable;
    [UsedImplicitly] public static IAppCore Instance 
        => _instanceNullable ?? throw new NullReferenceException($"{nameof(_instanceNullable)} is null");
    
    [UsedImplicitly] public static bool IsDesignMode { get; private set; }
    [UsedImplicitly] public static IServiceProvider Services { get; private set; } = null!;
    [UsedImplicitly] public static TService GetService<TService>() where TService : notnull 
        => Services.GetRequiredService<TService>();
    [UsedImplicitly] public static object GetService(Type serviceType) => Services.GetRequiredService(serviceType);

    [UsedImplicitly] public static object ApplicationLifetime { get; private set; } = null!;
    [UsedImplicitly] public static ICoreLifetime ApplicationLifetimeWrapped { get; private set; } = null!;
    [UsedImplicitly] public static bool IsClassicDesktopStyleApplicationLifetime { get; private set; }

    #endregion
}