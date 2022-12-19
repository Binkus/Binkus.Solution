namespace DDS.Core.Helper;

public static class PerformanceLogger
{
    private const bool UseOnlyDebugWriteLine = false;
    
    public static readonly Action<string> LogAction = UseOnlyDebugWriteLine ?
#if DEBUG
        s => Debug.WriteLine(s) :
#else
        s => { } :
#endif
        Console.WriteLine;
    
    public static void LogTime(this long timestamp, string msg)
        => LogAction($"{msg}:{timestamp.GetElapsedMilliseconds()}ms");
    
    public static void LogTime<T>(this long timestamp) where T : IPerformanceLoggerMarker<Action<long, string>>
        => T.LogAction?.Invoke(timestamp, T.LogMessage);

    public interface IPerformanceLoggerMarker : IPerformanceLoggerMarker<Action<long, string>> { }
    public interface IPerformanceLoggerMarker<TLogAction> where TLogAction : class
    {
        static virtual TLogAction? LogAction { get; } = ((Action<long, string>)LogTime as TLogAction);
        static abstract string LogMessage { get; }
    }
}


public class StartupPerformanceLoggerMarker : PerformanceLogger.IPerformanceLoggerMarker
{
    public static string LogMessage { get; } = "Startup";
}