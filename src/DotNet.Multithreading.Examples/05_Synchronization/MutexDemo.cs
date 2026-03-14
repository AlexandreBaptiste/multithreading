// ============================================================
// Concept  : Mutex
// Summary  : Demonstrates mutual exclusion using Mutex, including thread-affinity rules and named mutexes for cross-process synchronization.
// When to use   : Cross-process synchronization or single-instance application enforcement.
// When NOT to use: Intra-process synchronization where lock/Monitor is lighter and faster.
// ============================================================

namespace DotNet.Multithreading.Examples.Synchronization;

/// <summary>
/// Demonstrates mutual exclusion using <see cref="Mutex"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Mutex vs lock:</b> A <see cref="Mutex"/> is a kernel-mode primitive, making it significantly
/// heavier than <c>lock</c> / <see cref="System.Threading.Monitor"/> which operate entirely in
/// user-mode when there is no contention. Prefer <c>lock</c> for intra-process synchronisation.
/// </para>
/// <para>
/// <b>Thread-affinity:</b> A <see cref="Mutex"/> must be released by the same thread that acquired
/// it. Releasing from a different thread throws <see cref="ApplicationException"/>.
/// </para>
/// <para>
/// <b>Named mutexes:</b> Pass a name to the <see cref="Mutex"/> constructor
/// (e.g., <c>new Mutex(false, "Global\\MyApp")</c>) to synchronise across process boundaries —
/// useful for single-instance application enforcement.
/// </para>
/// <para>
/// <b><see cref="AbandonedMutexException"/>:</b> If a thread exits without releasing a
/// <see cref="Mutex"/>, the next waiter receives an <see cref="AbandonedMutexException"/>.
/// Always release the mutex inside a <c>finally</c> block to avoid this.
/// </para>
/// </remarks>
public static class MutexDemo
{
    /// <summary>
    /// Spawns <paramref name="threadCount"/> threads, each acquiring an unnamed <see cref="Mutex"/>,
    /// incrementing a shared counter, and releasing the mutex. Returns the final counter value.
    /// </summary>
    /// <param name="threadCount">Number of threads to spawn; each increments the counter once.</param>
    /// <returns>
    /// The final counter value, guaranteed to equal <paramref name="threadCount"/> because
    /// all increments are serialised by the mutex.
    /// </returns>
    /// <example>
    /// <code>
    /// int result = MutexDemo.ProtectCriticalSection(5);
    /// // result == 5
    /// </code>
    /// </example>
    public static int ProtectCriticalSection(int threadCount)
    {
        using Mutex mutex = new();
        int counter = 0;

        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                mutex.WaitOne();

                try
                {
                    counter++;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            });
        }

        foreach (Thread t in threads)
        {
            t.Start();
        }

        foreach (Thread t in threads)
        {
            t.Join();
        }

        return counter;
    }

    /// <summary>
    /// Demonstrates <see cref="WaitHandle.WaitOne(int)"/> with a timeout.
    /// A background thread acquires the mutex and holds it for 200 ms while the calling
    /// thread attempts to acquire it within <paramref name="timeoutMs"/> milliseconds.
    /// </summary>
    /// <param name="timeoutMs">
    /// Maximum milliseconds the calling thread will wait. Use a value less than 200 (e.g., 50)
    /// to reliably observe a timeout.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the mutex was acquired within the timeout;
    /// <see langword="false"/> if the timeout elapsed first.
    /// </returns>
    /// <example>
    /// <code>
    /// bool acquired = MutexDemo.TryAcquireWithTimeout(50);
    /// // acquired == false — mutex was held by background thread
    /// </code>
    /// </example>
    public static bool TryAcquireWithTimeout(int timeoutMs)
    {
        using Mutex mutex = new();
        ManualResetEventSlim mutexHeld = new(false);

        Thread holder = new(() =>
        {
            mutex.WaitOne();

            try
            {
                mutexHeld.Set();
                Thread.Sleep(200);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        });

        holder.Start();
        mutexHeld.Wait(); // ensure holder has the mutex before we try

        bool acquired = mutex.WaitOne(timeoutMs);

        if (acquired)
        {
            mutex.ReleaseMutex();
        }

        holder.Join();

        return acquired;
    }
}
