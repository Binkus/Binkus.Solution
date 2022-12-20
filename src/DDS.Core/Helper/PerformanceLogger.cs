using System.Collections.Concurrent;
using System.Collections.ObjectModel;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable MemberCanBePrivate.Global

namespace DDS.Core.Helper;

public static class PerformanceLogger
{
    private const bool UseOnlyDebugWriteLine = true;
    
#pragma warning disable CS0162
    public static readonly Action<string> LogAction = UseOnlyDebugWriteLine ?
#if DEBUG
        s => Debug.WriteLine(s) :
#else
        s => { } :
#endif
        Console.WriteLine;
#pragma warning restore CS0162

    public static DurationLogEntry LogTime(this long timestamp, string msg, bool print = true, bool saveResult = false, string? key = null)
    {
        var duration = timestamp.GetElapsedTime();
        return duration.LogTime(msg, print, saveResult, key);
    }

    public static DurationLogEntry LogTime<T>(this long timestamp, bool print = true, bool saveResult = false) where T : IPerformanceLoggerMarker
    {
        return T.LogTimestampAction.Invoke(timestamp, T.LogMessage, print, saveResult, typeof(T).FullName);
    }

    public static DurationLogEntry LogTime(this TimeSpan duration, string msg, bool print = true, bool saveResult = false, string? key = null)
    {
        // if (print) LogAction($"{msg} took:{duration.TotalMilliseconds}ms");
        // if (!saveResult) return duration.ToDurationLogEntry(msg, key);
        // UpdatablePerformanceLogs.AddOrUpdate(key ?? msg, _ => duration, (_, _) => duration);
        // return duration.ToDurationLogEntry(msg, key);
        
        var log = duration.ToDurationLogEntry(msg, key);
        if (print) log.Print();
        if (!saveResult) return log;
        Save(in log);
        return log;
    }

    public static DurationLogEntry LogTime<T>(this TimeSpan duration, bool print = true, bool saveResult = false) where T : IPerformanceLoggerMarker
    {
        return T.LogDurationAction.Invoke(duration, T.LogMessage, print, saveResult, typeof(T).FullName);
    }

    private static readonly ConcurrentDictionary<string, TimeSpan> UpdatablePerformanceLogs = new();

    public static void ClearLogs() => UpdatablePerformanceLogs.Clear(); 

    public static ReadOnlyDictionary<string, TimeSpan> PerformanceLogs { get; } = new(UpdatablePerformanceLogs);

    public static TimeSpan? TryGetResult(string key) => PerformanceLogs.TryGetValue(key, out var result) ? result : null;
    public static TimeSpan? TryGetResult<T>() where T : IPerformanceLoggerMarker => TryGetResult(typeof(T).FullName!);

    private static DurationLogEntry ToDurationLogEntry(this TimeSpan duration, string msg, string? key = null)
        => new DurationLogEntry(duration, msg, key);

    public static DurationLogEntry Save(this in DurationLogEntry log)
    {
        var duration = log.TimeSpan;
        var key = log.Key.ToString();
        UpdatablePerformanceLogs.AddOrUpdate(key, _ => duration, (_, _) => duration);
        return log;
    }
    
    public static DurationLogEntry Print(this in DurationLogEntry log)
    {
        // var ms = log.Duration.TotalMilliseconds;
        // var msg = log.LogMessage;
        // LogAction($"{msg} took:{ms}ms");
        LogAction(log);
        return log;
    }

    //
    
    public readonly ref struct DurationLogEntry
    {
        public DurationLogEntry(TimeSpan timeSpan, string message, string? key = null, bool printsKey = false)
        {
            TimeSpan = timeSpan;
            Message = message;
            Key = key ?? message;
            PrintsKey = printsKey;
        }
        public void Deconstruct(out TimeSpan timeSpan, out string message, out string key)
        {
            timeSpan = TimeSpan;
            message = Message.ToString();
            key = Key.ToString();
        }
        
        public void Deconstruct(out TimeSpan timeSpan, out string message)
        {
            timeSpan = TimeSpan;
            message = Message.ToString();
        }
        public TimeSpan TimeSpan { get; init; }
        public ReadOnlySpan<char> Message { get; init; }
        public ReadOnlySpan<char> Key { get; init; }
        public bool PrintsKey { get; init; }

        public static implicit operator TimeSpan(DurationLogEntry _) => _.TimeSpan;
        public static implicit operator string(DurationLogEntry _) => _.ToString();

        public override string ToString()
        {
            var key = !PrintsKey || Key == Message ? "" : $"::{Key}";
            return $">>perfLog>> {Message}{key} took:{TimeSpan.TotalMilliseconds}ms";
        }
    }
    
    //

    public delegate DurationLogEntry LogTimestampDelegate(long timestamp, string msg, bool print = true, bool saveResult = false, string? key = null);
    public delegate DurationLogEntry LogDurationDelegate(TimeSpan duration, string msg, bool print = true, bool saveResult = false, string? key = null);

    public interface IPerformanceLoggerMarker
    {
        static virtual LogTimestampDelegate LogTimestampAction { get; } = LogTime;
        static virtual LogDurationDelegate LogDurationAction { get; } = LogTime;

        static abstract string LogMessage { get; }
    }
    
    //
    
    public abstract class StartupPerformance : IPerformanceLoggerMarker
    {
        private StartupPerformance() { }
        public static string LogMessage => "Init Startup";
    }
    
    public abstract class AfterSetupPerformance : IPerformanceLoggerMarker
    {
        private AfterSetupPerformance() { }
        public static string LogMessage => "After Setup Startup";
    }
    
    public abstract class AvaloniaStartupPerformance : IPerformanceLoggerMarker
    {
        private AvaloniaStartupPerformance() { }
        public static string LogMessage => "(Avalonia/ReactiveUI) Framework Startup";
    }
    
    public abstract class MauiStartupPerformance : IPerformanceLoggerMarker
    {
        private MauiStartupPerformance() { }
        public static string LogMessage => "(Maui) Framework Startup";
    }
    
    public abstract class DependencyInjectionPerformance : IPerformanceLoggerMarker
    {
        private DependencyInjectionPerformance() { }
        public static string LogMessage => "DI";
    }
    
    public abstract class TotalAppStartupPerformance : IPerformanceLoggerMarker
    {
        private TotalAppStartupPerformance() { }
        public static string LogMessage => "Total App Startup";
    }
    
    public abstract class MainViewsViewModelsStartupPerformance : IPerformanceLoggerMarker
    {
        private MainViewsViewModelsStartupPerformance() { }
        public static string LogMessage => "Main-Views/ViewModels Creation";
    }
}


