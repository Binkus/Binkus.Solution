using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable StringLiteralTypo
// ReSharper disable MemberCanBePrivate.Global

namespace DDS.Core.Helper;

[SuppressMessage("Roslynator", "RCS1213:Remove unused member declaration.")]
[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
public static class TimeSpanExtensions
{
    /// <summary>Awaitable TimeSpan</summary>
    /// <inheritdoc cref="Task.Delay(TimeSpan)"/>
    /// <returns>An awaiter instance of task that represents the time delay.</returns>
    public static TaskAwaiter GetAwaiter(this TimeSpan delay) => Task.Delay(delay).GetAwaiter();
    
    /// <summary>Awaitable TimeSpan with option to continueOnCapturedContext or not.</summary>
    /// <param name="delay"><inheritdoc cref="Task.Delay(TimeSpan)"/></param>
    /// <param name="continueOnCapturedContext">
    /// <see langword="true" /> to attempt to marshal the continuation back to the original context captured; otherwise, <see langword="false" />.</param>
    /// <returns>An object used to await this task that represents the time delay.</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException"><inheritdoc cref="Task.Delay(TimeSpan)"/></exception>
    public static ConfiguredTaskAwaitable ConfigureAwait(this TimeSpan delay, bool continueOnCapturedContext)
    {
        return Task.Delay(delay).ConfigureAwait(continueOnCapturedContext);
    }
    
    /// <inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)"/>
    public static Task Delay(this TimeSpan delay, CancellationToken cancellationToken = default)
        => Task.Delay(delay, cancellationToken);
    
    /// <summary>
    /// <p>Not continuing on captured context:
    /// No attempt to marshal the continuation back to the originally captured context.</p>
    /// <p>Equivalent to "Task.Delay(TimeSpan, CancellationToken).ConfigureAwait(false)"</p>
    /// <inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)"/>
    /// </summary>
    /// <inheritdoc cref="Task.Delay(TimeSpan, CancellationToken)"/>
    /// <returns>An object used to await this task that represents the time delay.</returns>
    public static ConfiguredTaskAwaitable DelayWithoutContinuingOnCapturedContext(
        this TimeSpan delay, CancellationToken cancellationToken = default)
        => Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    
    /// <summary>
    /// Creates a duration as TimeSpan which can be awaited by Task.Delay or directly through ext method
    /// <see cref="GetAwaiter(TimeSpan)"/>.
    /// <p>E.g. <see langword="await 5.Seconds()"/> or <see langword="5.s()"/>
    /// which is equal to <see langword="await Task.Delay(TimeSpan.FromSeconds(5)"/>.</p>
    /// <p>Alternatively you can use ConfigureAwait(false) if you do not wish to continueOnCapturedContext,
    /// e.g. by <see langword="await 5.Seconds().ConfigureAwait(false)"/>.</p>
    /// <p>When awaiting the timespan with the custom awaiter <see cref="GetAwaiter(System.TimeSpan)"/> the timespan has to be positive,
    /// or <see langword="TimeSpan.FromMilliseconds(-1)" /> or equiv. <see langword="-1.ms()" /> to wait indefinitely.</p>
    /// <p><b>Examples:</b></p>
    /// <p><see langword="await 0.5.Hours(cancellationToken).ConfigureAwait(false);"/></p>
    /// <p><see langword="await 4.2.s(cancellationToken).ConfigureAwait(false);"/></p>
    /// <p><see langword="await 42.ms(cancellationToken);"/></p>
    /// <p><see langword="await 42.s().ConfigureAwait(false);"/></p>
    /// <p><see langword="await 42.s().Delay(cancellationToken).ConfigureAwait(false);"/></p>
    /// <p><see langword="await 42.s().DelayWithoutContinuingOnCapturedContext(cancellationToken);"/></p>
    /// </summary>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///        When awaited Task.<see cref="Task.Delay(TimeSpan,CancellationToken)"/> will be called, only then this exception can throw. When
    ///        <paramref name="timespan" /> represents a negative time interval other than <see langword="TimeSpan.FromMilliseconds(-1)" />.
    /// 
    /// -or-
    /// 
    /// The <paramref name="timespan" /> argument's <see cref="P:System.TimeSpan.TotalMilliseconds" /> property is greater than 4294967294 on .NET 6 and later versions, or <see cref="F:System.Int32.MaxValue">Int32.MaxValue</see> on all previous versions.</exception>
    /// <returns>TimeSpan / Duration / Delay (or with CancellationToken <see cref="TimeSpanCancellationToken"/>) of specified unit</returns>
    /// <seealso cref="Task.Delay(TimeSpan)"/>
    /// <seealso cref="Delay(TimeSpan, CancellationToken)"/>
    /// <seealso cref="DelayWithoutContinuingOnCapturedContext(TimeSpan, CancellationToken)"/>
    /// 
    /// <seealso cref="GetAwaiter(TimeSpan)"/>
    /// <seealso cref="ConfigureAwait(TimeSpan, bool)"/>
    /// 
    /// <seealso cref="TimeSpanCancellationToken"/>
    /// <seealso cref="TimeSpanCancellationToken.GetAwaiter()"/>
    /// 
    /// <seealso cref="Seconds(int)"/>
    /// <seealso cref="Seconds(double)"/>
    /// <seealso cref="s(int)"/>
    /// <seealso cref="s(double)"/>
    /// <seealso cref="Milliseconds(int)"/>
    /// <seealso cref="Milliseconds(double)"/>
    /// <seealso cref="ms(int)"/>
    /// <seealso cref="ms(double)"/>
    /// <seealso cref="Microseconds(int)"/>
    /// <seealso cref="Microseconds(double)"/>
    /// <seealso cref="Hours(int)"/>
    /// <seealso cref="Hours(double)"/>
    /// <seealso cref="Days(int)"/>
    /// <seealso cref="Days(double)"/>
    /// <seealso cref="Minutes(int)"/>
    /// <seealso cref="Minutes(double)"/>
    /// <seealso cref="Ticks(int)"/>
    /// <seealso cref="Ticks(long)"/>
    ///
    /// <seealso cref="Seconds(int, CancellationToken)"/>
    /// <seealso cref="Seconds(double, CancellationToken)"/>
    /// <seealso cref="s(int, CancellationToken)"/>
    /// <seealso cref="s(double, CancellationToken)"/>
    /// <seealso cref="Milliseconds(int, CancellationToken)"/>
    /// <seealso cref="Milliseconds(double, CancellationToken)"/>
    /// <seealso cref="ms(int, CancellationToken)"/>
    /// <seealso cref="ms(double, CancellationToken)"/>
    /// <seealso cref="Microseconds(int, CancellationToken)"/>
    /// <seealso cref="Microseconds(double, CancellationToken)"/>
    /// <seealso cref="Hours(int, CancellationToken)"/>
    /// <seealso cref="Hours(double, CancellationToken)"/>
    /// <seealso cref="Days(int, CancellationToken)"/>
    /// <seealso cref="Days(double, CancellationToken)"/>
    /// <seealso cref="Minutes(int, CancellationToken)"/>
    /// <seealso cref="Minutes(double, CancellationToken)"/>
    /// <seealso cref="Ticks(int, CancellationToken)"/>
    /// <seealso cref="Ticks(long, CancellationToken)"/>
    private static TimeSpan NumbersExtToTimeSpanDocs(TimeSpan timespan = default) => TimeSpan.Zero;
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Seconds(this double seconds) => TimeSpan.FromSeconds(seconds);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Milliseconds(this int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Milliseconds(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Microseconds(this int microseconds) => TimeSpan.FromMicroseconds(microseconds);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Microseconds(this double microseconds) => TimeSpan.FromMicroseconds(microseconds);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Hours(this int hours) => TimeSpan.FromHours(hours);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Hours(this double hours) => TimeSpan.FromHours(hours);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Days(this int days) => TimeSpan.FromDays(days);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Days(this double days) => TimeSpan.FromDays(days);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Minutes(this int minutes) => TimeSpan.FromMinutes(minutes);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Minutes(this double minutes) => TimeSpan.FromMinutes(minutes);

    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Ticks(this int ticks) => TimeSpan.FromTicks(ticks);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpan Ticks(this long ticks) => TimeSpan.FromTicks(ticks);
    
    // Abbreviations
    
    /// <inheritdoc cref="Milliseconds(int)"/>
    public static TimeSpan ms(this int milliseconds) => Milliseconds(milliseconds);
    /// <inheritdoc cref="Milliseconds(double)"/>
    public static TimeSpan ms(this double milliseconds) => Milliseconds(milliseconds);
    
    /// <inheritdoc cref="Seconds(int)"/>
    public static TimeSpan s(this int seconds) => Seconds(seconds);
    /// <inheritdoc cref="Seconds(double)"/>
    public static TimeSpan s(this double seconds) => Seconds(seconds);
    
    //
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Seconds(this int seconds, CancellationToken cancellationToken) => TimeSpan.FromSeconds(seconds).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Seconds(this double seconds, CancellationToken cancellationToken) => TimeSpan.FromSeconds(seconds).ToTimeSpanCancellationToken(cancellationToken);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Milliseconds(this int milliseconds, CancellationToken cancellationToken) => TimeSpan.FromMilliseconds(milliseconds).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Milliseconds(this double milliseconds, CancellationToken cancellationToken) => TimeSpan.FromMilliseconds(milliseconds).ToTimeSpanCancellationToken(cancellationToken);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Microseconds(this int microseconds, CancellationToken cancellationToken) => TimeSpan.FromMicroseconds(microseconds).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Microseconds(this double microseconds, CancellationToken cancellationToken) => TimeSpan.FromMicroseconds(microseconds).ToTimeSpanCancellationToken(cancellationToken);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Hours(this int hours, CancellationToken cancellationToken) => TimeSpan.FromHours(hours).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Hours(this double hours, CancellationToken cancellationToken) => TimeSpan.FromHours(hours).ToTimeSpanCancellationToken(cancellationToken);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Days(this int days, CancellationToken cancellationToken) => TimeSpan.FromDays(days).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Days(this double days, CancellationToken cancellationToken) => TimeSpan.FromDays(days).ToTimeSpanCancellationToken(cancellationToken);
    
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Minutes(this int minutes, CancellationToken cancellationToken) => TimeSpan.FromMinutes(minutes).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Minutes(this double minutes, CancellationToken cancellationToken) => TimeSpan.FromMinutes(minutes).ToTimeSpanCancellationToken(cancellationToken);

    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Ticks(this int ticks, CancellationToken cancellationToken) => TimeSpan.FromTicks(ticks).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="NumbersExtToTimeSpanDocs"/>
    public static TimeSpanCancellationToken Ticks(this long ticks, CancellationToken cancellationToken) => TimeSpan.FromTicks(ticks).ToTimeSpanCancellationToken(cancellationToken);
    
    // Abbreviations
    
    /// <inheritdoc cref="Milliseconds(int)"/>
    public static TimeSpanCancellationToken ms(this int milliseconds, CancellationToken cancellationToken) => Milliseconds(milliseconds).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="Milliseconds(double)"/>
    public static TimeSpanCancellationToken ms(this double milliseconds, CancellationToken cancellationToken) => Milliseconds(milliseconds).ToTimeSpanCancellationToken(cancellationToken);
    
    /// <inheritdoc cref="Seconds(int)"/>
    public static TimeSpanCancellationToken s(this int seconds, CancellationToken cancellationToken) => Seconds(seconds).ToTimeSpanCancellationToken(cancellationToken);
    /// <inheritdoc cref="Seconds(double)"/>
    public static TimeSpanCancellationToken s(this double seconds, CancellationToken cancellationToken) => Seconds(seconds).ToTimeSpanCancellationToken(cancellationToken);
    
    //
    
    /// <summary>
    /// Creates TimeSpanCancellationToken readonly struct
    /// </summary>
    /// <param name="timeSpan">time span used as delay when awaited</param>
    /// <param name="cancellationToken">token to cancel the created TaskAwaiter when awaited</param>
    /// <returns></returns>
    private static TimeSpanCancellationToken ToTimeSpanCancellationToken(
        this TimeSpan timeSpan, CancellationToken cancellationToken) 
        => new TimeSpanCancellationToken(timeSpan, cancellationToken);
    
    /// <summary>
    /// This readonly record struct is like a (TimeSpan, CancellationToken) Tuple with custom Awaiter to be able to get
    /// awaited. Creating this does not await anything, you could await default(TimeSpanCancellationToken) which would
    /// be equivalent to await Task.Delay(default(TimeSpan),default(CancellationToken)); (would complete immediately).
    /// </summary>
    /// <param name="TimeSpan">The time span (used as delay when awaited) to wait when this struct gets awaited,
    /// or <see langword="TimeSpan.FromMilliseconds(-1)" /> to wait indefinitely.</param>
    /// <param name="CancellationToken">A cancellation token to observe when awaiting this struct
    /// while waiting for the task to complete.</param>
    public readonly record struct TimeSpanCancellationToken(TimeSpan TimeSpan, CancellationToken CancellationToken)
    {
        /// <summary>
        /// When token is default(CancellationToken) (or CancellationToken.None which are equivalent)
        /// would complete immediately when awaited, if proper non-default CancellationToken,
        /// when awaited it would wait indefinitely until canceled.
        /// </summary>
        /// <param name="cancellationToken">When default(CancellationToken) (or CancellationToken.None which are
        /// equivalent) would complete immediately when awaited, if proper CancellationToken,
        /// when awaited it would wait indefinitely until canceled.</param>
        /// <returns><see cref="TimeSpanCancellationToken"/></returns>
        public static TimeSpanCancellationToken IndefinitelyUntilCanceled(CancellationToken cancellationToken) 
            => cancellationToken;
        
        public static TimeSpanCancellationToken UnsafeIndefinitelyNonCancelableDeadLock 
            => new(-1.ms(), default);

        /// <summary>Awaitable TimeSpanCancellationToken</summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><inheritdoc cref="TimeSpanExtensions.Delay"/></exception>
        /// <returns>An awaiter instance of a cancelable task that represents the time delay of TimeSpan,
        /// cancelable through CancellationToken.</returns>
        public TaskAwaiter GetAwaiter() => TimeSpan.Delay(CancellationToken).GetAwaiter();

        /// <summary>Awaitable ConfiguredTaskAwaitable with option to continueOnCapturedContext or not,
        /// when awaited calling <see cref="Task.Delay(System.TimeSpan,System.Threading.CancellationToken)"/> with
        /// <see cref="TimeSpan"/>
        /// and <see cref="CancellationToken"/>.</summary>
        /// <param name="continueOnCapturedContext">
        /// <see langword="true" /> to attempt to marshal the continuation back to the original context captured; otherwise, <see langword="false" />.</param>
        /// <returns>An object used to await this task that represents the time delay.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><inheritdoc cref="Task.Delay(System.TimeSpan)"/></exception>
        public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) =>
            Task.Delay(TimeSpan, CancellationToken).ConfigureAwait(continueOnCapturedContext);

        ///<inheritdoc cref="Task.Delay(System.TimeSpan,System.Threading.CancellationToken)"/>
        public Task Delay() => Task.Delay(TimeSpan, CancellationToken);
        
        public static implicit operator TimeSpan(TimeSpanCancellationToken _) => _.TimeSpan;
        public static implicit operator CancellationToken(TimeSpanCancellationToken _) => _.CancellationToken;

        public static implicit operator TimeSpanCancellationToken(TimeSpan _) => new(_, CancellationToken.None);
        
        /// <summary>
        /// When token is default(CancellationToken) (or CancellationToken.None which are equivalent)
        /// would complete immediately when awaited, if proper non-default CancellationToken,
        /// when awaited it would wait indefinitely until canceled.
        /// </summary>
        /// <param name="token">When default(CancellationToken) (or CancellationToken.None which are equivalent)
        /// would complete immediately when awaited, if proper CancellationToken,
        /// when awaited it would wait indefinitely until canceled.</param>
        /// <returns><see cref="TimeSpanCancellationToken"/></returns>
        public static implicit operator TimeSpanCancellationToken(CancellationToken token) 
            => token == default ? default : new TimeSpanCancellationToken(TimeSpan.FromMilliseconds(-1), token);
        
        public static implicit operator TimeSpanCancellationToken((TimeSpan timeSpan, CancellationToken token) tuple)
            => new(tuple.timeSpan, tuple.token);
        public static implicit operator TimeSpanCancellationToken((CancellationToken token, TimeSpan timeSpan) tuple)
            => new(tuple.timeSpan, tuple.token);
        public static implicit operator (CancellationToken token, TimeSpan timeSpan)(TimeSpanCancellationToken _) 
            => (_.CancellationToken, _.TimeSpan);
        public static implicit operator (TimeSpan timeSpan, CancellationToken token)(TimeSpanCancellationToken _) 
            => (_.TimeSpan, _.CancellationToken);

        // public static implicit operator Task(TimeSpanCancellationToken _) => _.Delay();
    }
    
    //
    
    ///<inheritdoc cref="Delay"/> // todo docs
    public static TaskAwaiter GetAwaiter(this (TimeSpan timeSpan, CancellationToken token) delay) => Task.Delay(delay.timeSpan, delay.token).GetAwaiter();
    
    ///<inheritdoc cref="Delay"/> // todo docs
    public static TaskAwaiter GetAwaiter(this (CancellationToken token, TimeSpan timeSpan) delay) => Task.Delay(delay.timeSpan, delay.token).GetAwaiter();

    ///<inheritdoc cref="ConfigureAwait(System.TimeSpan,bool)"/> // todo docs
    public static ConfiguredTaskAwaitable ConfigureAwait(this (TimeSpan timeSpan, CancellationToken token) delay, bool continueOnCapturedContext)
    {
        return Task.Delay(delay.timeSpan, delay.token).ConfigureAwait(continueOnCapturedContext);
    }
    
    ///<inheritdoc cref="ConfigureAwait(System.TimeSpan,bool)"/> // todo docs
    public static ConfiguredTaskAwaitable ConfigureAwait(this (CancellationToken token, TimeSpan timeSpan) delay, bool continueOnCapturedContext)
    {
        return Task.Delay(delay.timeSpan, delay.token).ConfigureAwait(continueOnCapturedContext);
    }
}