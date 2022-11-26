using System.Runtime.CompilerServices;

namespace DDS.Core.Helper;

public static class TimeSpanExtensions
{
    public static TaskAwaiter GetAwaiter(this TimeSpan delay) => Task.Delay(delay).GetAwaiter();
    
    public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);

    public static TimeSpan Seconds(this double seconds) => TimeSpan.FromSeconds(seconds);
    
    public static TimeSpan Milliseconds(this int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
    public static TimeSpan Milliseconds(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
    
    public static TimeSpan Microseconds(this int microseconds) => TimeSpan.FromMicroseconds(microseconds);
    public static TimeSpan Microseconds(this double microseconds) => TimeSpan.FromMicroseconds(microseconds);
    
    public static TimeSpan Hours(this int hours) => TimeSpan.FromHours(hours);
    public static TimeSpan Hours(this double hours) => TimeSpan.FromHours(hours);
    
    public static TimeSpan Minutes(this int minutes) => TimeSpan.FromMinutes(minutes);
    public static TimeSpan Minutes(this double minutes) => TimeSpan.FromMinutes(minutes);

    public static TimeSpan Ticks(this int ticks) => TimeSpan.FromTicks(ticks);
    public static TimeSpan Ticks(this long ticks) => TimeSpan.FromTicks(ticks);
}