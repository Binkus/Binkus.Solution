using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace DDS.Core.Helper;

public static class PerformanceLogger
{
    private const bool UseOnlyDebugWriteLine = true;
    
    public static readonly Action<string> LogAction = UseOnlyDebugWriteLine ?
#if DEBUG
        s => Debug.WriteLine(s) :
#else
        s => { } :
#endif
        Console.WriteLine;
    
    public static TimeSpan LogTime(this long timestamp, string msg, bool saveResult = false, string? key = null)
    {
        var duration = timestamp.GetElapsedTime();
        return duration.LogTime(msg, saveResult, key);
    }

    public static TimeSpan LogTime<T>(this long timestamp, bool saveResult = false) where T : IPerformanceLoggerMarker
    {
        return T.LogTimestampAction.Invoke(timestamp, T.LogMessage, saveResult, typeof(T).FullName);
    }

    public static TimeSpan LogTime(this TimeSpan duration, string msg, bool saveResult = false, string? key = null)
    {
        var ms = duration.TotalMilliseconds;
        LogAction($"{msg} took:{ms}ms");
        if (!saveResult) return duration;
        UpdatablePerformanceLogs.AddOrUpdate(key ?? msg, _ => duration, (_, _) => duration);
        return duration;
    }

    public static TimeSpan LogTime<T>(this TimeSpan duration, bool saveResult = false) where T : IPerformanceLoggerMarker
    {
        return T.LogDurationAction.Invoke(duration, T.LogMessage, saveResult, typeof(T).FullName);
    }

    private static readonly ConcurrentDictionary<string, TimeSpan> UpdatablePerformanceLogs = new();

    public static void ClearLogs() => UpdatablePerformanceLogs.Clear(); 

    public static ReadOnlyDictionary<string, TimeSpan> PerformanceLogs { get; } = new(UpdatablePerformanceLogs);

    public static TimeSpan? TryGetResult(string key) => PerformanceLogs.TryGetValue(key, out var result) ? result : null;
    public static TimeSpan? TryGetResult<T>() where T : IPerformanceLoggerMarker => TryGetResult(T.LogMessage);
    
    //

    public delegate TimeSpan LogTimestampDelegate(long timestamp, string msg, bool saveResult = false, string? key = null);
    public delegate TimeSpan LogDurationDelegate(TimeSpan duration, string msg, bool saveResult = false, string? key = null);
    
    public interface IPerformanceLoggerMarker
    {
        static virtual LogTimestampDelegate LogTimestampAction { get; } = LogTime;
        static virtual LogDurationDelegate LogDurationAction { get; } = LogTime;

        static abstract string LogMessage { get; }
    }
    
    //
    
    public sealed class StartupPerformance : IPerformanceLoggerMarker
    {
        private StartupPerformance() { }
        public static string LogMessage => "Startup";
    }
    
    public sealed class AfterSetupPerformance : IPerformanceLoggerMarker
    {
        private AfterSetupPerformance() { }
        public static string LogMessage => "After Setup Startup";
    }
    
    public sealed class AvaloniaStartupPerformance : IPerformanceLoggerMarker
    {
        private AvaloniaStartupPerformance() { }
        public static string LogMessage => "(Avalonia/ReactiveUI) Framework Startup";
    }
    
    public sealed class MauiStartupPerformance : IPerformanceLoggerMarker
    {
        private MauiStartupPerformance() { }
        public static string LogMessage => "(Maui) Framework Startup";
    }
    
    public sealed class DependencyInjectionPerformance : IPerformanceLoggerMarker
    {
        private DependencyInjectionPerformance() { }
        public static string LogMessage => "DI";
    }
    
    public sealed class FullAppStartupPerformance : IPerformanceLoggerMarker
    {
        private FullAppStartupPerformance() { }
        public static string LogMessage => "Full App Startup";
    }
}


