// ============================================================
// Concept  : Task Basics
// Summary  : Core Task APIs: Task.Run, Task<T>, Task.Factory.StartNew,
//            ContinueWith, TaskStatus, and AggregateException handling.
// When to use   : When offloading CPU-bound work to the ThreadPool,
//                 chaining continuations, or inspecting task lifecycle state.
// When NOT to use: Do not wrap inherently async I/O operations in Task.Run;
//                  call them directly with await. Avoid ContinueWith in
//                  modern code; use await for cleaner continuation chains.
// ============================================================

namespace DotNet.Multithreading.Examples.Tasks;

/// <summary>
/// Demonstrates fundamental <see cref="System.Threading.Tasks.Task"/> and
/// <see cref="System.Threading.Tasks.Task{TResult}"/> APIs:
/// <c>Task.Run</c>, <c>Task.Factory.StartNew</c>, <c>ContinueWith</c>,
/// <see cref="System.Threading.Tasks.TaskStatus"/>, and
/// <see cref="AggregateException"/> handling.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to use:</b> Offloading CPU-bound work to the ThreadPool while
/// retaining composability via <c>await</c>.
/// </para>
/// <para>
/// <b>When NOT to use:</b> Do not wrap already-async I/O operations in
/// <c>Task.Run</c>; call them directly with <c>await</c>.
/// </para>
/// </remarks>
public static class TaskBasics
{
    /// <summary>
    /// Offloads a simple computation to the ThreadPool via <c>Task.Run</c>,
    /// awaits the result, and returns the computed value.
    /// </summary>
    /// <returns>The computed integer result (<c>42</c>).</returns>
    public static async Task<int> RunTask()
    {
        int result = await Task.Run(() => 42);
        return result;
    }

    /// <summary>
    /// Creates a <see cref="System.Threading.Tasks.Task{TResult}">Task&lt;int&gt;</see>
    /// via <c>Task.Run</c>, awaits it, and returns the typed result.
    /// </summary>
    /// <returns>The integer value <c>100</c>.</returns>
    public static async Task<int> RunTaskWithResult()
    {
        Task<int> task = Task.Run(() => 100);
        int value = await task;
        return value;
    }

    /// <summary>
    /// Creates a dedicated OS thread for long-running work using
    /// <c>Task.Factory.StartNew</c> with
    /// <see cref="System.Threading.Tasks.TaskCreationOptions.LongRunning"/>,
    /// which bypasses the ThreadPool and allocates a real OS thread.
    /// </summary>
    /// <returns>
    /// The <see cref="System.Threading.Thread.ManagedThreadId"/> of the
    /// dedicated thread used to execute the task body.
    /// </returns>
    /// <remarks>
    /// The <c>LongRunning</c> hint tells the default scheduler to avoid the
    /// ThreadPool, making it suitable for work that would monopolise a pool
    /// thread for an extended period.
    /// </remarks>
    public static async Task<int> StartNewLongRunning()
    {
        Task<int> task = Task.Factory.StartNew(
            () => Thread.CurrentThread.ManagedThreadId,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        return await task;
    }

    /// <summary>
    /// Demonstrates <c>ContinueWith</c> by chaining a string-producing
    /// continuation onto an integer-producing antecedent task.
    /// </summary>
    /// <returns>
    /// A string constructed from the antecedent result, e.g.
    /// <c>"Result: 7"</c>.
    /// </returns>
    /// <remarks>
    /// Prefer <c>await</c> over <c>ContinueWith</c> in modern code for
    /// better readability and correct synchronisation context handling.
    /// </remarks>
    public static async Task<string> ContinueWithExample()
    {
        Task<string> chained = Task.Run(() => 7)
            .ContinueWith(antecedent => $"Result: {antecedent.Result}");

        return await chained;
    }

    /// <summary>
    /// Captures the <see cref="System.Threading.Tasks.TaskStatus"/> of a
    /// <see cref="System.Threading.Tasks.Task"/> before it is started and
    /// again after it completes.
    /// </summary>
    /// <returns>
    /// A tuple where <c>before</c> is <c>Created</c> (status prior to
    /// <c>Start()</c>) and <c>after</c> is <c>RanToCompletion</c> (status
    /// once the task is awaited).
    /// </returns>
    public static async Task<(TaskStatus before, TaskStatus after)> GetTaskStatus_BeforeAndAfter()
    {
        Task task = new(() => Thread.Sleep(1));

        TaskStatus before = task.Status;

        task.Start();
        await task;

        TaskStatus after = task.Status;
        return (before, after);
    }

    /// <summary>
    /// Demonstrates that <c>Task.Wait()</c> on a faulted task wraps the
    /// root cause in an <see cref="AggregateException"/>, and shows how to
    /// extract the inner exception message.
    /// </summary>
    /// <returns>
    /// The <see cref="Exception.Message"/> of the inner exception, which is
    /// <c>"task-fault"</c>.
    /// </returns>
    /// <remarks>
    /// Contrast this with <c>await</c>: awaiting a faulted task unwraps the
    /// <see cref="AggregateException"/> and rethrows the first inner exception
    /// directly, so no <see cref="AggregateException"/> catch is needed.
    /// </remarks>
    public static string HandleAggregateException()
    {
        Task faulted = Task.Run(
            () => throw new InvalidOperationException("task-fault"));

        try
        {
            faulted.Wait(); // throws AggregateException wrapping the inner exception
        }
        catch (AggregateException aex)
        {
            return aex.InnerExceptions[0].Message;
        }

        return string.Empty;
    }
}
