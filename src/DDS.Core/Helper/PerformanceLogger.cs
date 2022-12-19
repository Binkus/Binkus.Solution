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
    
    public static void LogTime(this long timestamp, string msg)
        => LogAction($"{msg}:{timestamp.GetElapsedMilliseconds()}ms");
    
    public static void LogTime<T>(this long timestamp) where T : IPerformanceLoggerMarker
        => T.LogAction.Invoke(timestamp, T.LogMessage);
    
    public interface IPerformanceLoggerMarker
    {
        static virtual Action<long, string> LogAction { get; } = LogTime;
        static abstract string LogMessage { get; }
    }
}


public class StartupPerformanceLoggerMarker : PerformanceLogger.IPerformanceLoggerMarker
{
    public static string LogMessage { get; } = "Startup";
}