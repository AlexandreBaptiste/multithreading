// ============================================================
// Concept  : Thread Starvation
// Summary  : A thread is perpetually denied access to a shared resource because other threads are always scheduled ahead of it
// When to use   : As a learning example — understand priority inversion and writer starvation with ReaderWriterLockSlim
// When NOT to use: In production code — avoid by keeping lock durations short, using fair scheduling, and avoiding thread-priority manipulation
// ============================================================

using System.Threading;

namespace DotNet.Multithreading.Examples.Pitfalls;

/// <summary>
/// Demonstrates thread starvation — a situation where a thread is continuously denied
/// access to a shared resource because other threads are always scheduled ahead of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Priority Inversion:</b> A high-priority thread holds the highest scheduling
/// preference but cannot run because the low-priority thread that owns a shared lock
/// is being preempted by medium-priority threads. Real-time operating systems use
/// <em>priority inheritance</em> to temporarily elevate the low-priority thread's
/// priority so it can release the lock faster.
/// </para>
/// <para>
/// <b>Writer Starvation:</b> A <c>ReaderWriterLockSlim</c> allows multiple concurrent
/// readers but only one writer. If readers continually arrive, a writer waiting for
/// exclusive access may never obtain it. <c>ReaderWriterLockSlim</c> in .NET mitigates
/// this with internal fairness heuristics, but the risk exists when reader throughput
/// is very high.
/// </para>
/// </remarks>
public static class ThreadStarvationDemo
{
    /// <summary>
    /// Demonstrates the priority-inversion scenario using <c>ThreadPriority</c>.
    /// A low-priority thread holds a lock; a high-priority thread needs that lock;
    /// a medium-priority thread keeps preempting the low-priority thread, delaying
    /// the lock release and starving the high-priority thread.
    /// </summary>
    /// <returns>
    /// The string <c>"demonstrated"</c> after all three threads have completed,
    /// confirming the priority-inversion scenario ran to completion.
    /// </returns>
    /// <example>
    /// <code>
    /// string result = ThreadStarvationDemo.PriorityInversion();
    /// // result == "demonstrated"
    /// </code>
    /// </example>
    public static string PriorityInversion()
    {
        object sharedLock = new object();

        // Low-priority thread acquires the lock and holds it for a while.
        Thread lowPriority = new Thread(() =>
        {
            lock (sharedLock)
                Thread.Sleep(200); // Simulates work done while holding the lock
        })
        { Priority = ThreadPriority.Lowest };

        // Medium-priority thread performs CPU-bound work, preempting the low-priority
        // thread and delaying the lock release that the high-priority thread needs.
        Thread mediumPriority = new Thread(() =>
        {
            Thread.SpinWait(50_000_000); // CPU-bound: starves the low-priority thread
        })
        { Priority = ThreadPriority.Normal };

        // High-priority thread needs the same lock but must wait for the low-priority
        // thread to release it — a classic priority-inversion stall.
        Thread highPriority = new Thread(() =>
        {
            lock (sharedLock) { } // Waits for low-priority to release; high priority starves
        })
        { Priority = ThreadPriority.Highest };

        lowPriority.Start();
        Thread.Sleep(10); // Ensure low-priority grabs the lock first

        mediumPriority.Start();
        highPriority.Start();

        highPriority.Join();
        mediumPriority.Join();
        lowPriority.Join();

        return "demonstrated";
    }

    /// <summary>
    /// Demonstrates potential writer starvation with <c>ReaderWriterLockSlim</c>.
    /// Several reader threads continuously acquire and release the read lock; a single
    /// writer thread attempts to acquire the write lock using <c>TryEnterWriteLock</c>
    /// with a 100 ms timeout.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the writer acquired the write lock within 100 ms
    /// (the built-in fairness heuristics of <c>ReaderWriterLockSlim</c> may allow this);
    /// <see langword="false"/> if the writer was starved and the timeout elapsed.
    /// </returns>
    /// <example>
    /// <code>
    /// bool writerSucceeded = ThreadStarvationDemo.WriterStarvationWithRWLock();
    /// // Result depends on reader pressure vs. built-in fairness heuristics
    /// </code>
    /// </example>
    public static bool WriterStarvationWithRWLock()
    {
        using ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
        using CancellationTokenSource cts = new CancellationTokenSource();

        // Four concurrent readers continuously hold the read lock.
        Thread[] readers = new Thread[4];

        for (int i = 0; i < readers.Length; i++)
        {
            readers[i] = new Thread(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    rwLock.EnterReadLock();
                    try
                    {
                        Thread.Sleep(10); // Hold the read lock briefly
                    }
                    finally
                    {
                        rwLock.ExitReadLock();
                    }
                }
            })
            { IsBackground = true };

            readers[i].Start();
        }

        Thread.Sleep(20); // Let readers establish continuous read pressure

        // Writer attempts to acquire exclusive access; may be starved by readers.
        bool writerSucceeded = rwLock.TryEnterWriteLock(100);

        if (writerSucceeded)
            rwLock.ExitWriteLock();

        cts.Cancel();

        foreach (Thread reader in readers)
            reader.Join(2000); // Wait for readers to observe cancellation and exit

        return writerSucceeded;
    }
}
