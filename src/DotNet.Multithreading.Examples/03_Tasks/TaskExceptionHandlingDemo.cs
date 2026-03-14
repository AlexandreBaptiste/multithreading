// ============================================================
// Concept  : Task Exception Handling
// Summary  : Catching, flattening, and filtering AggregateException;
//            distinguishing faulted tasks from cancelled tasks.
// When to use   : When consuming tasks via Task.WaitAll / Task.Result
//                 (blocking path) or when multiple concurrent tasks may
//                 each throw independently.
// When NOT to use: If you always await a single task, catching specific
//                  exception types directly is cleaner — await unwraps
//                  AggregateException automatically.
// ============================================================

namespace DotNet.Multithreading.Examples.Tasks;

/// <summary>
/// Demonstrates advanced <see cref="AggregateException"/> patterns:
/// catching, flattening nested instances, using
/// <see cref="AggregateException.Handle(Func{Exception, bool})"/>, and
/// distinguishing <see cref="System.Threading.Tasks.TaskStatus.Faulted"/>
/// from <see cref="System.Threading.Tasks.TaskStatus.Canceled"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to use:</b> Tasks awaited via <c>Task.WaitAll</c>,
/// <c>task.Result</c>, or <c>task.Wait()</c> wrap exceptions in an
/// <see cref="AggregateException"/>; catch it with the patterns shown here.
/// </para>
/// <para>
/// <b>When NOT to use:</b> If you <c>await</c> a single task directly,
/// the runtime unwraps the inner exception — no <see cref="AggregateException"/>
/// handling needed.
/// </para>
/// </remarks>
public static class TaskExceptionHandlingDemo
{
    /// <summary>
    /// Creates a faulted <see cref="System.Threading.Tasks.Task"/>, blocks on
    /// it via <c>Wait()</c>, catches the wrapping
    /// <see cref="AggregateException"/>, and returns the inner exception
    /// message.
    /// </summary>
    /// <returns>
    /// The <see cref="Exception.Message"/> of the first inner exception,
    /// <c>"aggregate-fault"</c>.
    /// </returns>
    public static string CatchAggregateException()
    {
        Task faulted = Task.Run(
            () => throw new InvalidOperationException("aggregate-fault"));

        try
        {
            faulted.Wait(); // throws AggregateException
        }
        catch (AggregateException aex)
        {
            return aex.InnerExceptions[0].Message;
        }

        return string.Empty;
    }

    /// <summary>
    /// Constructs a nested <see cref="AggregateException"/> (an
    /// <see cref="AggregateException"/> that itself contains other
    /// <see cref="AggregateException"/> instances) and calls
    /// <see cref="AggregateException.Flatten"/> to produce a single-level
    /// collection of real leaf exceptions.
    /// </summary>
    /// <returns>
    /// The number of leaf inner exceptions after flattening, which is
    /// <c>3</c> in this example.
    /// </returns>
    public static int FlattenAggregateException()
    {
        AggregateException inner1 = new(
            new InvalidOperationException("leaf-1"),
            new ArgumentException("leaf-2"));

        AggregateException inner2 = new(
            new NotSupportedException("leaf-3"));

        AggregateException nested = new(inner1, inner2);

        AggregateException flat = nested.Flatten();
        return flat.InnerExceptions.Count;
    }

    /// <summary>
    /// Uses <see cref="AggregateException.Handle(Func{Exception, bool})"/> to
    /// selectively handle exceptions by type, counting how many were handled
    /// and how many remain unhandled.
    /// </summary>
    /// <returns>
    /// A tuple where <c>handled</c> is the number of exceptions accepted by
    /// the predicate and <c>unhandled</c> is the number of exceptions that
    /// caused <c>Handle</c> to rethrow.
    /// </returns>
    public static (int handled, int unhandled) HandleAggregateException()
    {
        Task[] tasks = new Task[]
        {
            Task.Run(() => throw new InvalidOperationException("handled-op")),
            Task.Run(() => throw new ArgumentException("handled-arg")),
            Task.Run(() => throw new NotSupportedException("unhandled-ns")),
        };

        int handled = 0;
        int unhandled = 0;

        try
        {
            Task.WaitAll(tasks);
        }
        catch (AggregateException aex)
        {
            try
            {
                aex.Handle(ex =>
                {
                    if (ex is InvalidOperationException or ArgumentException)
                    {
                        handled++;
                        return true;
                    }

                    return false;
                });
            }
            catch (AggregateException remaining)
            {
                unhandled = remaining.InnerExceptions.Count;
            }
        }

        return (handled, unhandled);
    }

    /// <summary>
    /// Creates an immediately cancelled task using
    /// <see cref="System.Threading.Tasks.Task.FromCanceled(CancellationToken)"/>
    /// and returns its <see cref="System.Threading.Tasks.Task.IsFaulted"/> and
    /// <see cref="System.Threading.Tasks.Task.IsCanceled"/> values to show
    /// that cancelled tasks are <em>not</em> faulted tasks.
    /// </summary>
    /// <returns>
    /// A tuple <c>(isFaulted: false, isCancelled: true)</c> demonstrating
    /// that a cancelled task has <c>IsCanceled == true</c> and
    /// <c>IsFaulted == false</c>.
    /// </returns>
    public static (bool isFaulted, bool isCancelled) FaultedVsCancelledTask()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        Task cancelledTask = Task.FromCanceled(cts.Token);

        return (cancelledTask.IsFaulted, cancelledTask.IsCanceled);
    }
}
