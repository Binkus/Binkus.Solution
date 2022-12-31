using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable StringLiteralTypo
// ReSharper disable MemberCanBePrivate.Global

namespace Binkus.Extensions;

public static class StopwatchExtensions
{
    private static readonly double s_tickFrequency = 10_000_000.0 / Stopwatch.Frequency; // e.g. ~== 0.01 (depending on hardware&OS)
    private static long GetTimestampCompatibleTicks(long ticks) => (long)(ticks / s_tickFrequency); // e.g. ~== ticks * 100L
    
#if NET7_0_OR_GREATER

    public static long AddTimestamp(this int ticks) => Stopwatch.GetTimestamp() - GetTimestampCompatibleTicks(ticks);
    public static long AddTimestamp(this long ticks) => Stopwatch.GetTimestamp() - GetTimestampCompatibleTicks(ticks);
    public static long AddTimestamp(this TimeSpan timeSpan) => Stopwatch.GetTimestamp() - GetTimestampCompatibleTicks(timeSpan.Ticks);
    
    public static TimeSpan GetElapsedTime(this long startingTimestamp) => Stopwatch.GetElapsedTime(startingTimestamp);
    public static TimeSpan GetElapsedTime(this long startingTimestamp, long endingTimestamp)
        => Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp);
    
    public static double GetElapsedMicroseconds(this long startingTimestamp)
        => Stopwatch.GetElapsedTime(startingTimestamp).TotalNanoseconds / 1_000d;
    public static double GetElapsedMicroseconds(this long startingTimestamp, long endingTimestamp)
        => Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp).TotalNanoseconds / 1_000d;
    
    public static double GetElapsedMilliseconds(this long startingTimestamp)
        => Stopwatch.GetElapsedTime(startingTimestamp).TotalNanoseconds / 1_000_000d;
    public static double GetElapsedMilliseconds(this long startingTimestamp, long endingTimestamp)
        => Stopwatch.GetElapsedTime(startingTimestamp, endingTimestamp).TotalNanoseconds / 1_000_000d;
    
    public static double GetElapsedSeconds(this long startingTimestamp)
        => startingTimestamp.GetElapsedMilliseconds() / 1_000d;
    public static double GetElapsedSeconds(this long startingTimestamp, long endingTimestamp)
        => startingTimestamp.GetElapsedMilliseconds(endingTimestamp) / 1_000d;
    
    public static double GetElapsedMinutes(this long startingTimestamp)
        => startingTimestamp.GetElapsedSeconds() / 60d;
    public static double GetElapsedMinutes(this long startingTimestamp, long endingTimestamp)
        => startingTimestamp.GetElapsedSeconds(endingTimestamp) / 60d;

#else // e.g. NETSTANDARD2_0 || NETSTANDARD2_1
    
    private static TimeSpan StopwatchGetElapsedTime(this long startingTimestamp, long endingTimestamp) =>
        new((long)((endingTimestamp - startingTimestamp) * s_tickFrequency));

    private static TimeSpan StopwatchGetElapsedTime(long startingTimestamp) =>
        StopwatchGetElapsedTime(startingTimestamp, Stopwatch.GetTimestamp());
    
    //

    public static long AddTimestamp(this int ticks) => Stopwatch.GetTimestamp() - GetTimestampCompatibleTicks(ticks);
    public static long AddTimestamp(this long ticks) => Stopwatch.GetTimestamp() - GetTimestampCompatibleTicks(ticks);
    public static long AddTimestamp(this TimeSpan timeSpan) => Stopwatch.GetTimestamp() - GetTimestampCompatibleTicks(timeSpan.Ticks);
    
    public static TimeSpan GetElapsedTime(this long startingTimestamp) => StopwatchGetElapsedTime(startingTimestamp);
    public static TimeSpan GetElapsedTime(this long startingTimestamp, long endingTimestamp)
        => StopwatchGetElapsedTime(startingTimestamp, endingTimestamp);
    
    public static double GetElapsedMilliseconds(this long startingTimestamp)
        => StopwatchGetElapsedTime(startingTimestamp).TotalMilliseconds;
    public static double GetElapsedMilliseconds(this long startingTimestamp, long endingTimestamp)
        => StopwatchGetElapsedTime(startingTimestamp, endingTimestamp).TotalMilliseconds;
    
    public static double GetElapsedSeconds(this long startingTimestamp)
        => startingTimestamp.GetElapsedMilliseconds() / 1_000d;
    public static double GetElapsedSeconds(this long startingTimestamp, long endingTimestamp)
        => startingTimestamp.GetElapsedMilliseconds(endingTimestamp) / 1_000d;
    
    public static double GetElapsedMinutes(this long startingTimestamp)
        => startingTimestamp.GetElapsedSeconds() / 60d;
    public static double GetElapsedMinutes(this long startingTimestamp, long endingTimestamp)
        => startingTimestamp.GetElapsedSeconds(endingTimestamp) / 60d;

#endif
}