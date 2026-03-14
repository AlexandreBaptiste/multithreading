// ============================================================
// Concept  : Thread Fundamentals
// Summary  : Core APIs for creating and managing OS-level threads in .NET
// When to use   : When you need explicit control over thread lifecycle,
//                 naming, or foreground/background behaviour.
// When NOT to use: For most work prefer Task / async-await / ThreadPool
//                  which are lighter-weight and more composable.
// ============================================================
using System.Diagnostics;
// Disambiguate: System.Diagnostics also defines a ThreadState type
using ThreadState = System.Threading.ThreadState;

namespace DotNet.Multithreading.Examples.Basics;

/// <summary>
/// Demonstrates the fundamental <see cref="Thread"/> class APIs:
/// creation via <c>new Thread(ThreadStart)</c>, <see cref="Thread.Start()"/>,
/// <see cref="Thread.Join()"/>, <see cref="Thread.Name"/>,
/// <see cref="Thread.IsBackground"/>, <see cref="Thread.ThreadState"/>,
/// <see cref="Thread.CurrentThread"/>, and <see cref="Thread.Sleep(int)"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to use:</b> When you need explicit control over thread lifecycle,
/// naming, or foreground/background behaviour.
/// </para>
/// <para>
/// <b>When NOT to use:</b> For CPU-bound or I/O-bound work, prefer
/// <see cref="System.Threading.Tasks.Task"/> and <c>async</c>/<c>await</c>,
/// or submit work to the <see cref="ThreadPool"/>.
/// </para>
/// </remarks>
public static class ThreadFundamentals
{
    /// <summary>
    /// Starts a worker thread that increments a shared counter, then blocks the
    /// calling thread with <see cref="Thread.Join()"/> until the worker finishes,
    /// and returns the final counter value.
    /// </summary>
    /// <returns>
    /// The counter value after the worker thread completes — always <c>1</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// int result = ThreadFundamentals.RunAndJoin();
    /// Console.WriteLine(result); // 1
    /// </code>
    /// </example>
    public static int RunAndJoin()
    {
        int counter = 0;

        Thread worker = new(() => counter++);
        worker.Start();
        worker.Join();

        return counter;
    }

    /// <summary>
    /// Creates a thread assigned the given <paramref name="name"/>, starts it,
    /// and returns the <see cref="Thread.Name"/> as observed via
    /// <see cref="Thread.CurrentThread"/> from inside the new thread.
    /// </summary>
    /// <param name="name">The name to assign to the new thread. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// The thread name read from inside the thread; matches <paramref name="name"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// string? observed = ThreadFundamentals.GetThreadName("Worker-1");
    /// Console.WriteLine(observed); // Worker-1
    /// </code>
    /// </example>
    public static string? GetThreadName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        string? observedName = null;
        using ManualResetEventSlim done = new(false);

        Thread worker = new(() =>
        {
            observedName = Thread.CurrentThread.Name;
            done.Set();
        })
        {
            Name = name
        };

        worker.Start();
        done.Wait();

        return observedName;
    }

    /// <summary>
    /// Returns the default <see cref="Thread.IsBackground"/> value for a newly
    /// created, not-yet-started thread.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> — new threads are foreground threads by default.
    /// The process will not exit until all foreground threads have finished.
    /// </returns>
    public static bool IsBackgroundDefault()
    {
        Thread thread = new(() => { });
        return thread.IsBackground;
    }

    /// <summary>
    /// Returns the <see cref="ThreadState"/> of a thread that has been
    /// constructed but never started.
    /// </summary>
    /// <returns><see cref="ThreadState.Unstarted"/>.</returns>
    public static ThreadState GetThreadStateBeforeStart()
    {
        Thread thread = new(() => { });
        return thread.ThreadState;
    }

    /// <summary>
    /// Creates a thread, starts it, waits for completion via
    /// <see cref="Thread.Join()"/>, and returns the resulting
    /// <see cref="ThreadState"/>.
    /// </summary>
    /// <returns><see cref="ThreadState.Stopped"/>.</returns>
    public static ThreadState GetThreadStateAfterJoin()
    {
        Thread thread = new(() => { });
        thread.Start();
        thread.Join();

        return thread.ThreadState;
    }

    /// <summary>
    /// Demonstrates the difference between foreground and background threads
    /// by reading <see cref="Thread.IsBackground"/> on each.
    /// </summary>
    /// <returns>
    /// A tuple where <c>ForegroundIsBackground</c> is <see langword="false"/>
    /// and <c>BackgroundIsBackground</c> is <see langword="true"/>.
    /// </returns>
    /// <remarks>
    /// Foreground threads (<see cref="Thread.IsBackground"/> = <see langword="false"/>)
    /// keep the process alive. Background threads are automatically abandoned when
    /// all foreground threads have exited, regardless of their state.
    /// </remarks>
    public static (bool ForegroundIsBackground, bool BackgroundIsBackground) ForegroundVsBackgroundBehavior()
    {
        Thread foreground = new(() => { }) { IsBackground = false };
        Thread background = new(() => { }) { IsBackground = true };

        return (foreground.IsBackground, background.IsBackground);
    }

    /// <summary>
    /// Demonstrates <see cref="Thread.Sleep(int)"/> by pausing the calling thread
    /// for <paramref name="milliseconds"/> milliseconds and returning the actual
    /// elapsed time measured with a <see cref="Stopwatch"/>.
    /// </summary>
    /// <param name="milliseconds">Duration to sleep in milliseconds (≥ 0).</param>
    /// <returns>Elapsed milliseconds as measured by a <see cref="Stopwatch"/>.</returns>
    /// <remarks>
    /// Inside <c>async</c> methods, prefer <see cref="System.Threading.Tasks.Task.Delay(int)"/>
    /// so that no thread is blocked during the delay.
    /// </remarks>
    public static long SleepCurrentThread(int milliseconds)
    {
        Stopwatch sw = Stopwatch.StartNew();
        Thread.Sleep(milliseconds);
        sw.Stop();

        return sw.ElapsedMilliseconds;
    }
}
