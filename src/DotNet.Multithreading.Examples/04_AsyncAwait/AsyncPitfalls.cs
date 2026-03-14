// ============================================================
// Concept  : Async Pitfalls
// Summary  : Common async/await mistakes and their correct alternatives:
//            async void, sequential vs parallel awaits, and
//            synchronous ValueTask fast-path.
// When to use   : Study and reference when reviewing async code for
//                 correctness and performance.
// When NOT to use: The "Broken" variants are intentionally wrong —
//                  never use async void for non-event-handler methods.
// ============================================================

using System.Diagnostics;

namespace DotNet.Multithreading.Examples.AsyncAwait;

/// <summary>
/// Demonstrates common <c>async</c>/<c>await</c> pitfalls and their
/// correct alternatives, shown side by side.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pitfall 1 — <c>async void</c>:</b> An <c>async void</c> method
/// cannot be awaited, so any exception it throws is raised directly on
/// the <see cref="System.Threading.SynchronizationContext"/> that was
/// active when the method started. In most application hosts this
/// crashes the process. Callers have no way to observe the exception
/// through a <c>try/catch</c>. Use <c>async Task</c> (or accept a
/// <c>Func&lt;Task&gt;</c>) instead so exceptions propagate normally
/// through the returned <c>Task</c>.
/// </para>
/// <para>
/// <b>Pitfall 2 — <c>.Result</c> / <c>.Wait()</c> deadlock:</b>
/// Blocking synchronously on a <c>Task</c> inside a thread that owns a
/// <see cref="System.Threading.SynchronizationContext"/> (e.g., a WinForms
/// UI thread or ASP.NET Classic request thread) causes a deadlock.
/// The awaited task's continuation is posted back to the
/// <c>SynchronizationContext</c> but can never run because the thread is
/// blocked waiting for the task to finish. Always <c>await</c> tasks; never
/// call <c>.Result</c> or <c>.Wait()</c> on them.
/// </para>
/// <para>
/// <b>Pitfall 3 — Sequential awaits:</b> Awaiting each task one after
/// another forces them to run serially even when they are independently
/// I/O-bound and could overlap. Use <c>Task.WhenAll</c> to start all
/// tasks concurrently and then await their combined completion.
/// </para>
/// </remarks>
public static class AsyncPitfalls
{
    /// <summary>
    /// <b>BROKEN pattern — do not use.</b>
    /// An <c>async void</c> method whose exception cannot be caught by
    /// callers. The exception escapes to the
    /// <see cref="System.Threading.SynchronizationContext"/> and typically
    /// crashes the process.
    /// </summary>
    /// <remarks>
    /// This method exists solely to illustrate the pitfall. There is no
    /// corresponding unit test because the exception cannot be caught in a
    /// reliable, cross-platform way from outside the method.
    /// Use <see cref="AsyncVoidFixed"/> as the correct alternative.
    /// </remarks>
#pragma warning disable CS1998 // Async method lacks 'await' operators — intentional demo
    public static async void AsyncVoidBroken()
#pragma warning restore CS1998
    {
        // In real code the exception would escape to the SynchronizationContext.
        throw new InvalidOperationException(
            "Callers of async void cannot observe this exception.");
    }

    /// <summary>
    /// <b>FIXED pattern.</b>
    /// Accepts an asynchronous delegate (<see cref="Func{TResult}"/>) so the
    /// returned <see cref="System.Threading.Tasks.Task"/> can be awaited and
    /// exceptions propagate normally to the caller.
    /// </summary>
    /// <param name="work">An asynchronous delegate to execute.</param>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task"/> that completes (or faults)
    /// when <paramref name="work"/> completes.
    /// </returns>
    /// <example>
    /// <code>
    /// await AsyncPitfalls.AsyncVoidFixed(async () =>
    /// {
    ///     await Task.Delay(1);
    ///     throw new InvalidOperationException("now this IS catchable");
    /// });
    /// </code>
    /// </example>
    public static async Task AsyncVoidFixed(Func<Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        await work();
    }

    /// <summary>
    /// <b>SLOW pattern.</b>
    /// Awaits <paramref name="count"/> tasks of
    /// <paramref name="delayMs"/> milliseconds each one after another,
    /// resulting in total elapsed time of approximately
    /// <c>count × delayMs</c> ms.
    /// </summary>
    /// <param name="delayMs">The delay in milliseconds for each task.</param>
    /// <param name="count">The number of tasks to run sequentially.</param>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task{TResult}"/> producing the
    /// elapsed wall-clock time in milliseconds.
    /// </returns>
    public static async Task<long> SequentialAwaits(int delayMs, int count)
    {
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            await Task.Delay(delayMs);
        }

        sw.Stop();

        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    /// <b>FAST pattern.</b>
    /// Starts all <paramref name="count"/> tasks concurrently and awaits
    /// their combined completion with <c>Task.WhenAll</c>.
    /// Total elapsed time is approximately <paramref name="delayMs"/> ms
    /// regardless of <paramref name="count"/>.
    /// </summary>
    /// <param name="delayMs">The delay in milliseconds for each task.</param>
    /// <param name="count">The number of tasks to run concurrently.</param>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task{TResult}"/> producing the
    /// elapsed wall-clock time in milliseconds.
    /// </returns>
    /// <example>
    /// <code>
    /// long ms = await AsyncPitfalls.ParallelAwaitsFixed(50, 4);
    /// // ms ≈ 50, not 200
    /// </code>
    /// </example>
    public static async Task<long> ParallelAwaitsFixed(int delayMs, int count)
    {
        Stopwatch sw = Stopwatch.StartNew();

        Task[] tasks = Enumerable.Range(0, count)
            .Select(_ => Task.Delay(delayMs))
            .ToArray();

        await Task.WhenAll(tasks);

        sw.Stop();

        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    /// Demonstrates the correct synchronous fast-path for
    /// <see cref="System.Threading.Tasks.ValueTask{TResult}"/>.
    /// Returns a result immediately without <c>await</c> and without
    /// allocating a state-machine object.
    /// </summary>
    /// <returns>
    /// A synchronously completed
    /// <see cref="System.Threading.Tasks.ValueTask{TResult}"/> containing
    /// <c>0</c>.
    /// </returns>
    /// <remarks>
    /// A method that never needs to yield can simply return
    /// <c>ValueTask.FromResult(value)</c>. Adding the <c>async</c> keyword
    /// would create a state machine unnecessarily. This pattern is common
    /// in high-throughput code paths (e.g., cache hits, in-memory
    /// operations) where avoiding heap allocation matters.
    /// </remarks>
    public static ValueTask<int> AsyncMethodThatNeverYields()
    {
        // No await needed — return the value synchronously with zero allocation.
        return ValueTask.FromResult(0);
    }
}
