// ============================================================
// Concept  : Thread Pool vs Raw Thread
// Summary  : Side-by-side comparison of spawning raw OS threads versus
//            queuing work into the managed ThreadPool for the same workload.
// When to use   : Use ThreadPool (or Task.Run) as the default for any
//                 short-lived, CPU-bound work — it reuses threads and avoids
//                 per-thread stack allocation cost.
// When NOT to use: Avoid raw Thread for short work; raw threads are justified
//                  only for long-running loops, dedicated background services,
//                  or when specific thread affinity, priority, or name matters.
// ============================================================

// Alias required: this file's namespace ends with 'ThreadPool', which would
// otherwise shadow System.Threading.ThreadPool for unqualified use.
using SysThreadPool = System.Threading.ThreadPool;

namespace DotNet.Multithreading.Examples.ThreadPool;

/// <summary>
/// Compares the cost and usage of raw <see cref="System.Threading.Thread"/>
/// objects against the managed <see cref="System.Threading.ThreadPool"/> by
/// performing identical computations with both approaches.
/// </summary>
/// <remarks>
/// <para>
/// <b>Raw Thread cost:</b> Each <see cref="System.Threading.Thread"/> allocates
/// roughly 1 MB of stack space and requires an OS-level context-switch entry.
/// Creating thousands of raw threads can exhaust virtual address space and
/// degrade scheduling performance.
/// </para>
/// <para>
/// <b>ThreadPool advantage:</b> Threads are pre-allocated and reused; the pool's
/// hill-climbing algorithm maintains an optimal thread count relative to
/// available CPU cores. For most short-lived work, <c>ThreadPool.QueueUserWorkItem</c>
/// (or <c>Task.Run</c>) is the correct default.
/// </para>
/// <para>
/// <b>When raw threads are justified:</b>
/// <list type="bullet">
///   <item>Long-running background loops (would block pool threads indefinitely).</item>
///   <item>Work requiring a specific <see cref="System.Threading.Thread.Priority"/>.</item>
///   <item>Work that must keep the process alive as a foreground thread.</item>
///   <item>Work needing a human-readable <see cref="System.Threading.Thread.Name"/>
///         for diagnostics tools.</item>
/// </list>
/// </para>
/// </remarks>
public static class ThreadPoolVsThreadDemo
{
    /// <summary>
    /// Spawns <paramref name="count"/> raw <see cref="System.Threading.Thread"/>
    /// objects, each computing the sum of integers from 1 to 1 000, joins all
    /// threads, and returns the accumulated total.
    /// </summary>
    /// <param name="count">Number of threads to spawn. Must be ≥ 0.</param>
    /// <returns>
    /// The sum of all per-thread computations. For every thread the local sum is
    /// 500 500 (= 1 + 2 + … + 1 000), so the total equals
    /// <c><paramref name="count"/> × 500 500</c>.
    /// </returns>
    /// <remarks>
    /// Each <see cref="System.Threading.Thread"/> is started immediately after
    /// construction. <see cref="System.Threading.Thread.Join()"/> blocks the
    /// caller until every thread has finished before the total is returned.
    /// <see cref="Interlocked.Add(ref long, long)"/> guards the shared
    /// accumulator against data races.
    /// </remarks>
    /// <example>
    /// <code>
    /// long total = ThreadPoolVsThreadDemo.RunWithRawThreads(10);
    /// Console.WriteLine(total); // 5_005_000
    /// </code>
    /// </example>
    public static long RunWithRawThreads(int count)
    {
        if (count == 0)
        {
            return 0L;
        }

        long total = 0;
        System.Threading.Thread[] threads = new System.Threading.Thread[count];

        for (int i = 0; i < count; i++)
        {
            threads[i] = new System.Threading.Thread(() =>
            {
                long sum = 0;
                for (int j = 1; j <= 1_000; j++)
                {
                    sum += j;
                }

                Interlocked.Add(ref total, sum);
            });

            threads[i].Start();
        }

        foreach (System.Threading.Thread t in threads)
        {
            t.Join();
        }

        return total;
    }

    /// <summary>
    /// Queues <paramref name="count"/> work items to the thread pool, each
    /// computing the sum of integers from 1 to 1 000, waits for all items to
    /// complete, and returns the accumulated total.
    /// </summary>
    /// <param name="count">Number of work items to queue. Must be ≥ 0.</param>
    /// <returns>
    /// The same value as <see cref="RunWithRawThreads"/> for an equal
    /// <paramref name="count"/>: <c><paramref name="count"/> × 500 500</c>.
    /// </returns>
    /// <remarks>
    /// A <see cref="CountdownEvent"/> with an initial count of
    /// <paramref name="count"/> is used instead of <c>Thread.Sleep</c> to
    /// synchronise the caller. Each work item signals the event on completion;
    /// the caller blocks at <see cref="CountdownEvent.Wait()"/> until the count
    /// reaches zero.
    /// </remarks>
    /// <example>
    /// <code>
    /// long total = ThreadPoolVsThreadDemo.RunWithThreadPool(10);
    /// Console.WriteLine(total); // 5_005_000
    /// </code>
    /// </example>
    public static long RunWithThreadPool(int count)
    {
        if (count == 0)
        {
            return 0L;
        }

        long total = 0;
        using CountdownEvent countdown = new(count);

        for (int i = 0; i < count; i++)
        {
            SysThreadPool.QueueUserWorkItem(_ =>
            {
                long sum = 0;
                for (int j = 1; j <= 1_000; j++)
                {
                    sum += j;
                }

                Interlocked.Add(ref total, sum);
                countdown.Signal();
            });
        }

        countdown.Wait();
        return total;
    }
}
