// ============================================================
// Concept  : SemaphoreSlim
// Summary  : Demonstrates concurrency throttling using SemaphoreSlim, including async waiting and resource limiting.
// When to use   : Limiting concurrent access to a resource pool or throttling async workloads.
// When NOT to use: Simple mutual exclusion (use lock) or cross-process scenarios (use Semaphore).
// ============================================================

namespace DotNet.Multithreading.Examples.Synchronization;

/// <summary>
/// Demonstrates concurrency throttling using <see cref="SemaphoreSlim"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>SemaphoreSlim vs Semaphore:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="SemaphoreSlim"/> is a lightweight, user-mode semaphore designed for
///       use within a single process. It supports <c>async/await</c> via
///       <see cref="SemaphoreSlim.WaitAsync()"/>, making it the preferred choice for
///       throttling asynchronous workloads.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="System.Threading.Semaphore"/> is a kernel-mode primitive that supports
///       named semaphores for cross-process synchronisation, but has no async support and
///       carries heavier overhead.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="SemaphoreSlim"/> whenever you need to limit the number of concurrent
/// operations — for example, capping outbound HTTP requests or database connections.
/// </para>
/// </remarks>
public static class SemaphoreDemo
{
    /// <summary>
    /// Runs <paramref name="totalTasks"/> concurrent tasks, throttled so that at most
    /// <paramref name="maxConcurrent"/> tasks execute simultaneously, and returns the peak
    /// concurrency level observed during execution.
    /// </summary>
    /// <param name="totalTasks">Total number of tasks to schedule.</param>
    /// <param name="maxConcurrent">Maximum number of tasks allowed to run at the same time.</param>
    /// <returns>
    /// The peak number of concurrently active tasks; always &lt;= <paramref name="maxConcurrent"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int peak = await SemaphoreDemo.ThrottleConcurrency(20, 4);
    /// // peak &lt;= 4
    /// </code>
    /// </example>
    public static async Task<int> ThrottleConcurrency(int totalTasks, int maxConcurrent)
    {
        using SemaphoreSlim semaphore = new(maxConcurrent, maxConcurrent);

        int activeCount = 0;
        int peakCount = 0;

        Task[] tasks = new Task[totalTasks];

        for (int i = 0; i < totalTasks; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    int current = Interlocked.Increment(ref activeCount);

                    // Record peak using a compare-exchange loop to avoid a race on peakCount.
                    int observed;
                    do
                    {
                        observed = peakCount;
                        if (current <= observed)
                        {
                            break;
                        }
                    }
                    while (Interlocked.CompareExchange(ref peakCount, current, observed) != observed);

                    await Task.Delay(10).ConfigureAwait(false);

                    Interlocked.Decrement(ref activeCount);
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return peakCount;
    }

    /// <summary>
    /// Acquires a slot from a <see cref="SemaphoreSlim"/> asynchronously and releases it,
    /// demonstrating the basic <see cref="SemaphoreSlim.WaitAsync()"/> / <see cref="SemaphoreSlim.Release()"/> pattern.
    /// </summary>
    /// <param name="initialCount">
    /// The initial (and maximum) count of the semaphore. A value of 1 makes it behave like a mutex.
    /// </param>
    /// <returns><see langword="true"/> if the slot was acquired and released successfully.</returns>
    /// <example>
    /// <code>
    /// bool ok = await SemaphoreDemo.WaitAsyncExample(1);
    /// // ok == true
    /// </code>
    /// </example>
    public static async Task<bool> WaitAsyncExample(int initialCount)
    {
        using SemaphoreSlim semaphore = new(initialCount, initialCount);

        await semaphore.WaitAsync().ConfigureAwait(false);
        semaphore.Release();

        return true;
    }
}
