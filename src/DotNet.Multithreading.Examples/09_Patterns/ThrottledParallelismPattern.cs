// ============================================================
// Concept  : Throttled parallelism — "process N items with max M concurrent"
// Summary  : Correct pattern uses SemaphoreSlim + Task.WhenAll so that
//            exactly M tasks are in-flight at any moment.
// Common mistake: Parallel.ForEach with async lambdas does NOT await them —
//                 it fires all tasks without waiting, giving unbounded
//                 concurrency and ignoring async results entirely.
// ============================================================

namespace DotNet.Multithreading.Examples.Patterns;

/// <summary>
/// Demonstrates how to cap the number of concurrently executing async operations
/// using <c>SemaphoreSlim</c> combined with <c>Task.WhenAll</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why not <c>Parallel.ForEach</c> with async lambdas?</b>
/// <c>Parallel.ForEach</c> is designed for CPU-bound, synchronous delegates.
/// When you pass an <c>async</c> lambda it receives a <c>Task</c> back from
/// the delegate but does <em>not</em> await it — so every item is started
/// immediately without any concurrency limit and the results are lost.
/// </para>
/// <para>
/// <b>Correct pattern:</b>
/// Wrap each work item in an async delegate that acquires a <c>SemaphoreSlim</c>
/// slot before proceeding and releases it in a <c>finally</c> block. Collect all
/// the resulting <c>Task&lt;T&gt;</c> instances and <c>await Task.WhenAll</c> to
/// preserve result ordering while capping in-flight concurrency.
/// </para>
/// </remarks>
public static class ThrottledParallelismPattern
{
    /// <summary>
    /// Processes every element in <paramref name="items"/> concurrently, with at
    /// most <paramref name="maxConcurrent"/> operations executing at the same time.
    /// Results are returned in the same order as the input array.
    /// </summary>
    /// <param name="items">The source items to process.</param>
    /// <param name="maxConcurrent">Maximum number of concurrent operations.</param>
    /// <param name="processor">
    /// An async transform function applied to each item.
    /// </param>
    /// <param name="ct">Cancellation token for cooperative shutdown.</param>
    /// <returns>
    /// An array of results in the same order as <paramref name="items"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int[] results = await ThrottledParallelismPattern.ProcessWithThrottle(
    ///     items: Enumerable.Range(1, 10).ToArray(),
    ///     maxConcurrent: 3,
    ///     processor: async item =>
    ///     {
    ///         await Task.Delay(50);
    ///         return item * 2;
    ///     });
    /// </code>
    /// </example>
    public static async Task<int[]> ProcessWithThrottle(
        int[] items,
        int maxConcurrent,
        Func<int, Task<int>> processor,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(processor);

        SemaphoreSlim sem = new(maxConcurrent, maxConcurrent);

        Task<int>[] tasks = items.Select(async item =>
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                return await processor(item).ConfigureAwait(false);
            }
            finally
            {
                sem.Release();
            }
        }).ToArray();

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Measures the peak number of concurrently executing operations when
    /// <paramref name="totalItems"/> items are processed with a limit of
    /// <paramref name="maxConcurrent"/> concurrent tasks.
    /// </summary>
    /// <param name="totalItems">Total number of items to process.</param>
    /// <param name="maxConcurrent">Maximum allowed concurrent operations.</param>
    /// <returns>
    /// The highest observed concurrency level; always ≤ <paramref name="maxConcurrent"/>.
    /// </returns>
    public static async Task<int> MeasurePeakConcurrency(int totalItems, int maxConcurrent)
    {
        int current = 0;
        int peak = 0;

        await ProcessWithThrottle(
            items: Enumerable.Range(1, totalItems).ToArray(),
            maxConcurrent: maxConcurrent,
            processor: async item =>
            {
                int concurrency = Interlocked.Increment(ref current);

                // Record the peak using a compare-exchange loop
                int observed = Volatile.Read(ref peak);

                while (concurrency > observed)
                {
                    int previous = Interlocked.CompareExchange(ref peak, concurrency, observed);

                    if (previous == observed)
                    {
                        break;
                    }

                    observed = previous;
                }

                await Task.Delay(20).ConfigureAwait(false);

                Interlocked.Decrement(ref current);

                return item;
            }).ConfigureAwait(false);

        return peak;
    }
}
