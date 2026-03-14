// ============================================================
// Concept  : Async/Await Basics
// Summary  : Core async/await patterns: async Task<T>, ValueTask<T>,
//            ConfigureAwait(false), and already-completed tasks.
// When to use   : I/O-bound operations, avoiding thread blocking,
//                 and composing asynchronous workflows with clean
//                 control flow and no callback nesting.
// When NOT to use: Do not use ConfigureAwait(false) in UI code that
//                  must resume on the UI thread after an await.
//                  Do not wrap pure CPU-bound work in async without
//                  Task.Run — it only adds overhead.
// ============================================================

namespace DotNet.Multithreading.Examples.AsyncAwait;

/// <summary>
/// Demonstrates core <c>async</c>/<c>await</c> patterns in .NET:
/// <see cref="System.Threading.Tasks.Task{TResult}"/>,
/// <see cref="System.Threading.Tasks.ValueTask{TResult}"/>,
/// <c>ConfigureAwait(false)</c>, and already-completed tasks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Async State Machine:</b> The C# compiler transforms every
/// <c>async</c> method into a struct that implements
/// <c>IAsyncStateMachine</c>. Each <c>await</c> expression becomes a
/// resumption point; the runtime schedules the continuation after the
/// awaited operation completes. The state-machine struct is heap-promoted
/// only when the method actually suspends — fast-path synchronous
/// completions avoid the allocation entirely.
/// </para>
/// <para>
/// <b><c>ValueTask&lt;T&gt;</c> rationale:</b> Unlike
/// <see cref="System.Threading.Tasks.Task{TResult}"/>,
/// <see cref="System.Threading.Tasks.ValueTask{TResult}"/> is a value
/// type. When a method completes synchronously — the "fast path" —
/// returning <c>ValueTask.FromResult(value)</c> avoids a heap allocation
/// entirely. Only prefer <c>ValueTask</c> when profiling shows measurable
/// <c>Task</c> allocation pressure in the hot path.
/// </para>
/// <para>
/// <b><c>ConfigureAwait(false)</c>:</b> By default, <c>await</c>
/// captures the current
/// <see cref="System.Threading.SynchronizationContext"/> (or
/// <see cref="System.Threading.Tasks.TaskScheduler"/>) and resumes the
/// continuation on it. In library code this is unnecessary overhead and
/// can cause deadlocks when a caller blocks synchronously (e.g., calling
/// <c>.Result</c> on a WinForms or ASP.NET Classic message-loop thread).
/// Calling <c>ConfigureAwait(false)</c> opts out of context capture and
/// allows the continuation to run on any thread-pool thread.
/// </para>
/// <para>
/// <b>When NOT to use <c>ConfigureAwait(false)</c>:</b> In UI application
/// code (WPF, WinForms, Blazor Server) where the code after <c>await</c>
/// must access UI controls or <c>HttpContext</c>. UI controls have thread
/// affinity; resuming on a thread-pool thread throws an exception.
/// </para>
/// </remarks>
public static class AsyncAwaitBasics
{
    /// <summary>
    /// Demonstrates a standard <c>async Task&lt;int&gt;</c> method.
    /// Awaits a short delay to simulate I/O and then returns a value.
    /// </summary>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task{TResult}"/> that produces
    /// the integer <c>42</c> after a brief asynchronous delay.
    /// </returns>
    /// <example>
    /// <code>
    /// int result = await AsyncAwaitBasics.ComputeAsync();
    /// // result == 42
    /// </code>
    /// </example>
    public static async Task<int> ComputeAsync()
    {
        await Task.Delay(1);

        return 42;
    }

    /// <summary>
    /// Demonstrates the <see cref="System.Threading.Tasks.ValueTask{TResult}"/>
    /// fast-path optimisation.
    /// </summary>
    /// <param name="fast">
    /// <see langword="true"/> to take the synchronous fast path, returning
    /// a pre-computed <c>ValueTask</c> with zero heap allocation.
    /// <see langword="false"/> to exercise the asynchronous slow path
    /// that awaits a <c>Task.Delay</c>.
    /// </param>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.ValueTask{TResult}"/> that
    /// produces <c>42</c> on the fast path or <c>99</c> on the slow path.
    /// </returns>
    /// <remarks>
    /// The method is intentionally <b>not</b> marked <c>async</c> so that
    /// the fast path (<paramref name="fast"/> == <see langword="true"/>)
    /// returns <c>ValueTask.FromResult(42)</c> without ever allocating a
    /// state-machine object. Only the slow path delegates to an <c>async</c>
    /// local function.
    /// </remarks>
    /// <example>
    /// <code>
    /// int fast = await AsyncAwaitBasics.ComputeSynchronouslyFastPath(true);
    /// // fast == 42, no heap allocation
    ///
    /// int slow = await AsyncAwaitBasics.ComputeSynchronouslyFastPath(false);
    /// // slow == 99, awaited async path
    /// </code>
    /// </example>
    public static ValueTask<int> ComputeSynchronouslyFastPath(bool fast)
    {
        if (fast)
        {
            // Zero allocation: ValueTask<T> is a struct wrapping the value directly.
            return ValueTask.FromResult(42);
        }

        // Delegate to a real async local function only when we need to await.
        return new ValueTask<int>(SlowPathCoreAsync());

        static async Task<int> SlowPathCoreAsync()
        {
            await Task.Delay(1);

            return 99;
        }
    }

    /// <summary>
    /// Demonstrates the use of <c>ConfigureAwait(false)</c> to opt out of
    /// <see cref="System.Threading.SynchronizationContext"/> capture.
    /// Recommended in library code to avoid deadlocks and reduce scheduling
    /// overhead.
    /// </summary>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task{TResult}"/> that produces
    /// <see langword="true"/> once the delay completes.
    /// </returns>
    /// <example>
    /// <code>
    /// bool ok = await AsyncAwaitBasics.WithConfigureAwaitFalse();
    /// // ok == true
    /// </code>
    /// </example>
    public static async Task<bool> WithConfigureAwaitFalse()
    {
        // ConfigureAwait(false) tells the runtime NOT to marshal the continuation
        // back to the captured SynchronizationContext — safe in library code.
        await Task.Delay(1).ConfigureAwait(false);

        return true;
    }

    /// <summary>
    /// Shows that awaiting
    /// <see cref="System.Threading.Tasks.Task.CompletedTask"/> is a no-op:
    /// the task is already in the <c>RanToCompletion</c> state so the
    /// continuation runs synchronously without ever yielding to the scheduler.
    /// </summary>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task{TResult}"/> that produces
    /// the string <c>"done"</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// string result = await AsyncAwaitBasics.AlreadyCompletedTask();
    /// // result == "done"
    /// </code>
    /// </example>
    public static async Task<string> AlreadyCompletedTask()
    {
        // Task.CompletedTask is a cached singleton — awaiting it never suspends.
        await Task.CompletedTask;

        return "done";
    }
}
