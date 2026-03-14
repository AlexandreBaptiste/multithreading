// ============================================================
// Concept  : Thread Pool
// Summary  : Demonstrates the .NET ThreadPool for efficient work-item queuing
//            without the overhead of creating a dedicated OS thread per task.
// When to use   : Short-lived, CPU-bound work items that benefit from
//                 thread reuse; avoids the ~1 MB stack allocation and
//                 OS scheduling start-up cost of a raw Thread per item.
// When NOT to use: Long-running blocking work (can starve the pool),
//                  work requiring a specific thread name/priority/culture,
//                  or work that must stay as a foreground thread.
// ============================================================

// Alias required: this file lives in the namespace
// DotNet.Multithreading.Examples.ThreadPool, so the bare identifier
// 'ThreadPool' resolves to that namespace segment rather than
// System.Threading.ThreadPool without the alias below.
using SysThreadPool = System.Threading.ThreadPool;

namespace DotNet.Multithreading.Examples.ThreadPool;

/// <summary>
/// Demonstrates the .NET <see cref="System.Threading.ThreadPool"/> API:
/// queueing work items, passing state, querying thread counts, and
/// registering wait callbacks via
/// <c>ThreadPool.RegisterWaitForSingleObject</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread reuse:</b> The pool maintains a set of worker threads that are
/// recycled across work items, eliminating the ~1 MB stack allocation and
/// OS scheduling overhead of spawning a new <see cref="System.Threading.Thread"/>
/// per item.
/// </para>
/// <para>
/// <b>When to prefer ThreadPool:</b> Short CPU-bound tasks, I/O completion
/// callbacks, and quick computations where the start/stop overhead of a raw
/// thread would dominate.
/// </para>
/// <para>
/// <b>When to prefer raw Thread:</b> Long-running background loops, work that
/// needs a specific <see cref="System.Threading.Thread.Priority"/>, a human-readable
/// <see cref="System.Threading.Thread.Name"/>, or must remain a foreground thread.
/// </para>
/// </remarks>
public static class ThreadPoolDemo
{
    /// <summary>
    /// Queues <paramref name="count"/> work items to the thread pool using
    /// <see cref="System.Threading.ThreadPool.QueueUserWorkItem(WaitCallback)"/>
    /// and blocks until every item has completed.
    /// </summary>
    /// <param name="count">Number of work items to queue. Must be ≥ 0.</param>
    /// <returns>The number of work items that were executed.</returns>
    /// <remarks>
    /// A <see cref="CountdownEvent"/> synchronises the caller with all queued
    /// items — each item signals the event on completion and the caller blocks
    /// until the count reaches zero. This avoids the non-determinism of
    /// <c>Thread.Sleep</c>-based polling.
    /// </remarks>
    /// <example>
    /// <code>
    /// int executed = ThreadPoolDemo.QueueWorkItems(50);
    /// Console.WriteLine(executed); // 50
    /// </code>
    /// </example>
    public static int QueueWorkItems(int count)
    {
        if (count == 0)
        {
            return 0;
        }

        int executed = 0;
        using CountdownEvent countdown = new(count);

        for (int i = 0; i < count; i++)
        {
            SysThreadPool.QueueUserWorkItem(_ =>
            {
                Interlocked.Increment(ref executed);
                countdown.Signal();
            });
        }

        countdown.Wait();
        return executed;
    }

    /// <summary>
    /// Queues a single work item that receives <paramref name="state"/> as a
    /// typed <see cref="object"/> parameter and echoes it back as a
    /// <see langword="string"/>.
    /// </summary>
    /// <param name="state">The string payload to pass through the work item.</param>
    /// <returns>The same string value observed inside the work item.</returns>
    /// <remarks>
    /// The two-parameter overload of
    /// <see cref="System.Threading.ThreadPool.QueueUserWorkItem(WaitCallback, object?)"/>
    /// avoids a closure allocation by passing state directly through the
    /// thread-pool infrastructure rather than capturing it in a lambda.
    /// </remarks>
    /// <example>
    /// <code>
    /// string echo = ThreadPoolDemo.QueueWorkItemWithState("hello");
    /// Console.WriteLine(echo); // hello
    /// </code>
    /// </example>
    public static string QueueWorkItemWithState(string state)
    {
        ArgumentNullException.ThrowIfNull(state);

        using ManualResetEventSlim done = new(false);
        string captured = string.Empty;

        SysThreadPool.QueueUserWorkItem(
            s =>
            {
                captured = (string)s!;
                done.Set();
            },
            state);

        done.Wait();
        return captured;
    }

    /// <summary>
    /// Returns the current minimum and maximum thread counts for the thread
    /// pool's worker threads and I/O completion-port threads.
    /// </summary>
    /// <returns>
    /// A tuple of (<c>minWorker</c>, <c>minIO</c>, <c>maxWorker</c>,
    /// <c>maxIO</c>) as reported by
    /// <see cref="System.Threading.ThreadPool.GetMinThreads"/> and
    /// <see cref="System.Threading.ThreadPool.GetMaxThreads"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Minimum threads:</b> The pool keeps at least this many threads alive.
    /// Beyond the minimum, additional threads are injected using a hill-climbing
    /// algorithm that adds one thread per second until throughput stops improving.
    /// </para>
    /// <para>
    /// <b>Maximum threads:</b> The hard cap; work items queue when the pool is
    /// saturated. On modern machines this is typically
    /// <c>Environment.ProcessorCount × 250</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var (minW, minIO, maxW, maxIO) = ThreadPoolDemo.GetMinMaxThreads();
    /// Console.WriteLine($"Min workers: {minW}, Max workers: {maxW}");
    /// </code>
    /// </example>
    public static (int minWorker, int minIO, int maxWorker, int maxIO) GetMinMaxThreads()
    {
        SysThreadPool.GetMinThreads(out int minWorker, out int minIO);
        SysThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);
        return (minWorker, minIO, maxWorker, maxIO);
    }

    /// <summary>
    /// Registers a callback with
    /// <c>ThreadPool.RegisterWaitForSingleObject</c>,
    /// signals the underlying <see cref="AutoResetEvent"/> immediately, and
    /// returns <see langword="true"/> when the callback fires without timing out.
    /// </summary>
    /// <param name="timeoutMs">
    /// Timeout in milliseconds passed to
    /// <c>ThreadPool.RegisterWaitForSingleObject</c>.
    /// Use a value that is comfortably larger than the expected signal latency,
    /// e.g. <c>5_000</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the callback was invoked because the event was
    /// signalled (not because the timeout elapsed); <see langword="false"/>
    /// otherwise.
    /// </returns>
    /// <remarks>
    /// <c>ThreadPool.RegisterWaitForSingleObject</c> is
    /// ideal for timer callbacks and waiting on kernel objects without tying up a
    /// dedicated thread. Always call <see cref="RegisteredWaitHandle.Unregister"/>
    /// to release the underlying kernel wait handle and prevent resource leaks.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool fired = ThreadPoolDemo.RegisterWaitCallback(5_000);
    /// Console.WriteLine(fired); // True
    /// </code>
    /// </example>
    public static bool RegisterWaitCallback(int timeoutMs)
    {
        using AutoResetEvent trigger = new(false);
        using ManualResetEventSlim callbackCompleted = new(false);
        bool callbackFiredWithoutTimeout = false;

        RegisteredWaitHandle handle = SysThreadPool.RegisterWaitForSingleObject(
            trigger,
            (_, timedOut) =>
            {
                callbackFiredWithoutTimeout = !timedOut;
                callbackCompleted.Set();
            },
            null,
            timeoutMs,
            executeOnlyOnce: true);

        // Signal immediately so the callback fires with timedOut = false.
        trigger.Set();

        // Wait up to 4× the registered timeout for the callback to complete.
        callbackCompleted.Wait(timeoutMs * 4);
        handle.Unregister(null);

        return callbackFiredWithoutTimeout;
    }
}
