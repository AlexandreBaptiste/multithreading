// ============================================================
// Concept  : Task Combinators
// Summary  : Composing multiple tasks with WhenAll, WhenAny (async) and
//            WaitAll, WaitAny (blocking). Also demonstrates partial-failure
//            handling through AggregateException.
// When to use   : WhenAll/WhenAny — compose concurrent async operations
//                 without blocking the calling thread.
//                 WaitAll/WaitAny — only in non-async entry points where
//                 blocking is acceptable (e.g., Main method, legacy code).
// When NOT to use: Avoid WaitAll/WaitAny in async methods; they block a
//                  ThreadPool thread and risk deadlocks in environments
//                  with a single-threaded synchronisation context.
// ============================================================

namespace DotNet.Multithreading.Examples.Tasks;

/// <summary>
/// Demonstrates composing multiple tasks with async combinators
/// (<c>Task.WhenAll</c> / <c>Task.WhenAny</c>) and their blocking equivalents
/// (<c>Task.WaitAll</c> / <c>Task.WaitAny</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Async vs blocking:</b>
/// <c>Task.WhenAll</c> and <c>Task.WhenAny</c> return a <c>Task</c> that can
/// be <c>await</c>ed, freeing the calling thread while waiting.
/// <c>Task.WaitAll</c> and <c>Task.WaitAny</c> block the calling thread until
/// the condition is met — this is almost always the wrong choice in async code.
/// </para>
/// <para>
/// <b>Prefer async:</b> Always prefer <c>await Task.WhenAll</c> /
/// <c>await Task.WhenAny</c> in async contexts to avoid thread starvation and
/// potential deadlocks.
/// </para>
/// </remarks>
public static class TaskCombinators
{
    /// <summary>
    /// Fans out work across <paramref name="values"/> using <c>Task.Run</c>
    /// and collects all results with <c>Task.WhenAll</c>.
    /// </summary>
    /// <param name="values">The input integers to double. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// An array where each element is the corresponding input value multiplied
    /// by two, in the same order as <paramref name="values"/>.
    /// </returns>
    public static async Task<int[]> WhenAllExample(int[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        Task<int>[] tasks = values
            .Select(value => Task.Run(() => value * 2))
            .ToArray();

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Starts three tasks with delays of 10 ms, 50 ms, and 100 ms and returns
    /// the result value of the first task to complete via <c>Task.WhenAny</c>.
    /// </summary>
    /// <returns>
    /// <c>0</c> when the 10 ms task wins; <c>1</c> when the 50 ms task wins;
    /// <c>2</c> when the 100 ms task wins. Under normal conditions the 10 ms
    /// task wins and <c>0</c> is returned.
    /// </returns>
    public static async Task<int> WhenAnyExample()
    {
        Task<int>[] tasks = new Task<int>[]
        {
            Task.Run(async () => { await Task.Delay(10); return 0; }),
            Task.Run(async () => { await Task.Delay(50); return 1; }),
            Task.Run(async () => { await Task.Delay(100); return 2; }),
        };

        Task<int> firstCompleted = await Task.WhenAny(tasks);
        return await firstCompleted;
    }

    /// <summary>
    /// Starts three tasks where two succeed and one faults, then awaits
    /// <c>Task.WhenAll</c> to collect the partial failure. The number of
    /// inner exceptions captured in the resulting
    /// <see cref="AggregateException"/> is returned.
    /// </summary>
    /// <returns>
    /// The count of inner exceptions inside the <see cref="AggregateException"/>
    /// produced by <c>Task.WhenAll</c> — <c>1</c> when exactly one task throws.
    /// </returns>
    public static async Task<int> WhenAllWithPartialFailure()
    {
        Task[] tasks = new Task[]
        {
            Task.Run(() => { /* success */ }),
            Task.Run(() => { /* success */ }),
            Task.Run(() => throw new InvalidOperationException("one-failure")),
        };

        Task whenAllTask = Task.WhenAll(tasks);

        try
        {
            await whenAllTask;
        }
        catch
        {
            // await rethrows the first exception only; the full set lives in
            // whenAllTask.Exception which is an AggregateException.
        }

        return whenAllTask.Exception?.InnerExceptions.Count ?? 0;
    }

    /// <summary>
    /// Creates <paramref name="count"/> tasks via <c>Task.Run</c> and blocks
    /// the calling thread with <c>Task.WaitAll</c> until all complete, then
    /// returns their sum.
    /// </summary>
    /// <param name="count">
    /// The number of tasks to create. Tasks produce the values <c>1</c>
    /// through <paramref name="count"/> inclusive.
    /// </param>
    /// <returns>
    /// The sum of all task results, equal to
    /// <c>count × (count + 1) / 2</c>.
    /// </returns>
    /// <remarks>
    /// This method blocks the calling thread. Prefer <c>await Task.WhenAll</c>
    /// in async code.
    /// </remarks>
    public static int WaitAllBlocking(int count)
    {
        Task<int>[] tasks = Enumerable.Range(1, count)
            .Select(i => Task.Run(() => i))
            .ToArray();

        Task.WaitAll(tasks);

        return tasks.Sum(t => t.Result);
    }

    /// <summary>
    /// Starts three tasks with delays of 10 ms, 50 ms, and 100 ms and blocks
    /// the calling thread with <c>Task.WaitAny</c> until the first completes.
    /// </summary>
    /// <returns>
    /// The zero-based index into the task array of the first completed task.
    /// Under normal conditions returns <c>0</c> (the 10 ms task).
    /// </returns>
    /// <remarks>
    /// This method blocks the calling thread. Prefer <c>await Task.WhenAny</c>
    /// in async code.
    /// </remarks>
    public static int WaitAnyBlocking()
    {
        Task[] tasks = new Task[]
        {
            Task.Run(async () => await Task.Delay(10)),
            Task.Run(async () => await Task.Delay(50)),
            Task.Run(async () => await Task.Delay(100)),
        };

        return Task.WaitAny(tasks);
    }
}
