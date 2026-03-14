// ============================================================
// Concept  : Deadlock
// Summary  : Two or more threads each wait for a resource held by the other, causing all of them to block forever
// When to use   : As a learning example — understand the four Coffman conditions (mutual exclusion, hold-and-wait, no preemption, circular wait)
// When NOT to use: In production code — prevent via consistent lock ordering or timeout-based lock acquisition (Monitor.TryEnter)
// ============================================================

using System.Threading;

namespace DotNet.Multithreading.Examples.Pitfalls;

/// <summary>
/// Demonstrates deadlock — a situation where two or more threads permanently block each
/// other by each holding a resource the other needs.
/// </summary>
/// <remarks>
/// <para>
/// <b>Coffman conditions (all four must hold for a deadlock to occur):</b>
/// <list type="number">
///   <item><b>Mutual Exclusion</b> — only one thread may hold a given lock at a time.</item>
///   <item><b>Hold and Wait</b> — a thread holds a lock while waiting to acquire another.</item>
///   <item><b>No Preemption</b> — a lock can only be released voluntarily by its owner.</item>
///   <item><b>Circular Wait</b> — Thread A waits for Thread B's lock while Thread B waits for
///     Thread A's lock (ABBA ordering).</item>
/// </list>
/// </para>
/// <para>
/// <b>Prevention strategies used in .NET:</b>
/// <list type="bullet">
///   <item><b>Consistent lock ordering</b> — always acquire lockA before lockB everywhere.
///     Eliminates circular wait (condition 4).</item>
///   <item><b><c>Monitor.TryEnter</c> with timeout</b> — if a lock cannot be acquired within
///     a deadline the thread backs off, breaking hold-and-wait (condition 2) temporarily.</item>
/// </list>
/// </para>
/// </remarks>
public static class DeadlockDemo
{
    /// <summary>
    /// Demonstrates the classic ABBA lock-ordering deadlock.
    /// Thread 1 acquires <c>lockA</c> then attempts <c>lockB</c>;
    /// Thread 2 acquires <c>lockB</c> then attempts <c>lockA</c>.
    /// Both use <c>Monitor.TryEnter</c> with <paramref name="timeoutMs"/> so they time out
    /// instead of blocking forever.
    /// </summary>
    /// <param name="timeoutMs">
    /// Maximum milliseconds each thread will wait on the second lock before giving up.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if deadlock was detected (at least one <c>TryEnter</c> call
    /// timed out); <see langword="false"/> if both threads somehow completed without
    /// contention (unlikely under load).
    /// </returns>
    /// <example>
    /// <code>
    /// bool deadlocked = DeadlockDemo.Broken(500);
    /// // deadlocked is almost certainly true on a multi-core machine
    /// </code>
    /// </example>
    public static bool Broken(int timeoutMs)
    {
        object lockA = new object();
        object lockB = new object();
        int deadlockDetectedFlag = 0;

        // A CountdownEvent ensures both threads hold their first lock before either
        // attempts the second, guaranteeing the circular-wait condition is met.
        using CountdownEvent bothLocked = new CountdownEvent(2);

        Thread thread1 = new Thread(() =>
        {
            lock (lockA)
            {
                bothLocked.Signal(); // Thread1 has lockA
                bothLocked.Wait();   // Wait until Thread2 has lockB

                bool acquired = Monitor.TryEnter(lockB, timeoutMs);

                if (!acquired)
                    Interlocked.Exchange(ref deadlockDetectedFlag, 1); // Deadlock: timed out
                else
                    Monitor.Exit(lockB);
            }
        });

        Thread thread2 = new Thread(() =>
        {
            lock (lockB)
            {
                bothLocked.Signal(); // Thread2 has lockB
                bothLocked.Wait();   // Wait until Thread1 has lockA

                bool acquired = Monitor.TryEnter(lockA, timeoutMs);

                if (!acquired)
                    Interlocked.Exchange(ref deadlockDetectedFlag, 1); // Deadlock: timed out
                else
                    Monitor.Exit(lockA);
            }
        });

        thread1.Start();
        thread2.Start();
        thread1.Join();
        thread2.Join();

        return deadlockDetectedFlag == 1;
    }

    /// <summary>
    /// Demonstrates deadlock prevention through consistent lock ordering.
    /// Both threads always acquire <c>lockA</c> before <c>lockB</c>,
    /// eliminating the circular-wait Coffman condition.
    /// </summary>
    /// <param name="iterations">
    /// Number of critical-section entries each thread performs. Both threads
    /// together complete exactly <paramref name="iterations"/> entries total
    /// (split evenly).
    /// </param>
    /// <returns>
    /// The total number of critical-section entries completed, which equals
    /// <paramref name="iterations"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int completed = DeadlockDemo.Fixed(10);
    /// // completed == 10, no deadlock possible
    /// </code>
    /// </example>
    public static int Fixed(int iterations)
    {
        object lockA = new object();
        object lockB = new object();
        int completed = 0;

        int half = iterations / 2;
        int rest = iterations - half;

        Thread thread1 = new Thread(() =>
        {
            for (int i = 0; i < half; i++)
            {
                // Consistent ordering: always lockA → lockB
                lock (lockA)
                lock (lockB)
                    completed++;
            }
        });

        Thread thread2 = new Thread(() =>
        {
            for (int i = 0; i < rest; i++)
            {
                // Same ordering as thread1: lockA → lockB (no circular wait possible)
                lock (lockA)
                lock (lockB)
                    completed++;
            }
        });

        thread1.Start();
        thread2.Start();
        thread1.Join();
        thread2.Join();

        return completed;
    }

    /// <summary>
    /// Demonstrates using <c>Monitor.TryEnter</c> with a timeout as a deadlock escape
    /// hatch. The main thread holds a lock; a spawned thread attempts to acquire the same
    /// lock but times out because it is already held.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> because the spawned thread cannot acquire the lock within
    /// the 100 ms timeout — the lock is held for the entire duration by the calling thread.
    /// </returns>
    /// <example>
    /// <code>
    /// bool acquired = DeadlockDemo.TryEnterEscapeHatch();
    /// // acquired == false: TryEnter timed out, deadlock was avoided
    /// </code>
    /// </example>
    public static bool TryEnterEscapeHatch()
    {
        object resource = new object();
        bool acquired = false;

        // The main thread holds `resource`; the spawned thread demonstrates
        // that TryEnter returns false when the lock cannot be acquired within
        // the timeout, allowing the thread to back off gracefully.
        lock (resource)
        {
            Thread blocker = new Thread(() =>
            {
                // Will time out after 100 ms because the calling thread holds `resource`.
                acquired = Monitor.TryEnter(resource, 100);

                if (acquired)
                    Monitor.Exit(resource);
            });

            blocker.Start();
            blocker.Join(); // Join inside the lock — safe because blocker uses TryEnter
        }

        return acquired; // false: TryEnter timed out
    }
}
