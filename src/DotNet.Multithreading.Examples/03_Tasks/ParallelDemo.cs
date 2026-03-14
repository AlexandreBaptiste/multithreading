// ============================================================
// Concept  : Parallel Programming — Parallel.For / ForEach / PLINQ
// Summary  : Data-parallel operations using Parallel.For, Parallel.ForEach,
//            Parallel.ForEachAsync, and PLINQ.
// When to use   : Computationally intensive loops over independent data
//                 partitions where thread-safety is maintained (e.g. with
//                 Interlocked or local accumulators).
// When NOT to use: I/O-bound loops (use async/await), operations with
//                  significant shared state, or work that requires strict
//                  sequential ordering (unless AsOrdered() is acceptable).
// ============================================================

namespace DotNet.Multithreading.Examples.Tasks;

/// <summary>
/// Demonstrates data-parallel patterns using <c>Parallel.For</c>,
/// <c>Parallel.ForEach</c>, <c>Parallel.ForEachAsync</c>, and PLINQ.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to use:</b> CPU-bound loops over independent data partitions.
/// Use <see cref="System.Threading.Interlocked"/> operations or local
/// accumulators to avoid data races on shared state.
/// </para>
/// <para>
/// <b>When NOT to use:</b> I/O-bound workloads (use <c>async</c>/<c>await</c>)
/// or when strict sequential ordering is required.
/// </para>
/// </remarks>
public static class ParallelDemo
{
    /// <summary>
    /// Computes the sum of integers from <c>1</c> to <paramref name="n"/>
    /// (inclusive) using <c>Parallel.For</c> with
    /// <see cref="System.Threading.Interlocked.Add(ref int, int)"/> for
    /// thread-safe accumulation.
    /// </summary>
    /// <param name="n">The upper bound (inclusive). Must be positive.</param>
    /// <returns>
    /// The sum <c>1 + 2 + … + n</c>, equal to
    /// <c>n × (n + 1) / 2</c>.
    /// </returns>
    public static int ParallelForSum(int n)
    {
        int sum = 0;

        Parallel.For(1, n + 1, i => Interlocked.Add(ref sum, i));

        return sum;
    }

    /// <summary>
    /// Computes the sum of <paramref name="values"/> using
    /// <c>Parallel.ForEach</c> with
    /// <see cref="System.Threading.Interlocked.Add(ref int, int)"/> for
    /// thread-safe accumulation.
    /// </summary>
    /// <param name="values">The integers to sum. Must not be <see langword="null"/>.</param>
    /// <returns>The sum of all elements in <paramref name="values"/>.</returns>
    public static int ParallelForEachSum(IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        int sum = 0;

        Parallel.ForEach(values, item => Interlocked.Add(ref sum, item));

        return sum;
    }

    /// <summary>
    /// Computes the sum of <paramref name="values"/> using
    /// <c>Parallel.ForEach</c> with
    /// <see cref="ParallelOptions.MaxDegreeOfParallelism"/> capped at
    /// <paramref name="maxDegree"/> concurrent threads.
    /// </summary>
    /// <param name="values">The integers to sum. Must not be <see langword="null"/>.</param>
    /// <param name="maxDegree">
    /// The maximum number of concurrent threads. A value of <c>1</c> forces
    /// sequential execution.
    /// </param>
    /// <returns>The sum of all elements in <paramref name="values"/>.</returns>
    public static int ParallelForEachWithMaxDegree(int[] values, int maxDegree)
    {
        ArgumentNullException.ThrowIfNull(values);

        int sum = 0;
        ParallelOptions options = new() { MaxDegreeOfParallelism = maxDegree };

        Parallel.ForEach(values, options, item => Interlocked.Add(ref sum, item));

        return sum;
    }

    /// <summary>
    /// Computes the sum of <paramref name="values"/> using
    /// <c>Parallel.ForEachAsync</c>, which schedules asynchronous work items
    /// and returns a <see cref="System.Threading.Tasks.Task"/> that completes
    /// when all items have been processed.
    /// </summary>
    /// <param name="values">The integers to sum. Must not be <see langword="null"/>.</param>
    /// <returns>The sum of all elements in <paramref name="values"/>.</returns>
    /// <remarks>
    /// Although no genuine I/O is performed here, <c>Parallel.ForEachAsync</c>
    /// is intended for workloads where the body delegate performs async I/O.
    /// </remarks>
    public static async Task<int> ParallelForEachAsyncExample(int[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        int sum = 0;

        await Parallel.ForEachAsync(values, async (item, ct) =>
        {
            Interlocked.Add(ref sum, item);
            await ValueTask.CompletedTask;
        });

        return sum;
    }

    /// <summary>
    /// Computes the sum of integers from <c>1</c> to <paramref name="n"/>
    /// (inclusive) using PLINQ with <c>AsOrdered()</c> to preserve element
    /// order through the pipeline.
    /// </summary>
    /// <param name="n">The upper bound (inclusive). Must be positive.</param>
    /// <returns>
    /// The ordered sum equal to <c>n × (n + 1) / 2</c>.
    /// </returns>
    public static int PlinqOrderedSum(int n)
    {
        return Enumerable.Range(1, n)
            .AsParallel()
            .AsOrdered()
            .Select(x => x)
            .Sum();
    }

    /// <summary>
    /// Runs a PLINQ query over integers <c>1</c> to <paramref name="n"/>
    /// that respects the supplied <see cref="CancellationToken"/>
    /// via <c>WithCancellation</c>.
    /// </summary>
    /// <param name="n">The upper bound (inclusive). Must be positive.</param>
    /// <param name="ct">
    /// A token that, when cancelled, causes the query to throw
    /// <see cref="OperationCanceledException"/>.
    /// </param>
    /// <returns>The sum of <c>1</c> through <paramref name="n"/>.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled before or during
    /// query execution.
    /// </exception>
    public static int PlinqWithCancellation(int n, CancellationToken ct)
    {
        return Enumerable.Range(1, n)
            .AsParallel()
            .WithCancellation(ct)
            .Select(x => x)
            .Sum();
    }
}
