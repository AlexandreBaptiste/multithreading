// ============================================================
// Concept  : Livelock
// Summary  : Threads are actively running but make no progress because each keeps yielding to the other upon detecting contention
// When to use   : As a learning example — understand how symmetrical back-off can cause perpetual motion without useful work
// When NOT to use: In production code — break symmetry with randomised back-off so threads are unlikely to collide again
// ============================================================

using System;
using System.Threading;

namespace DotNet.Multithreading.Examples.Pitfalls;

/// <summary>
/// Demonstrates livelock — a concurrency pathology where threads are continuously active
/// but make no useful progress because each keeps yielding to the other.
/// </summary>
/// <remarks>
/// <para>
/// <b>Livelock definition:</b> Two or more threads react to each other's state changes
/// in a loop, each "politely" giving way to the other. Unlike a deadlock, the threads
/// are not suspended — they consume CPU time while accomplishing nothing.
/// </para>
/// <para>
/// <b>Distinguishing livelock from deadlock:</b>
/// <list type="bullet">
///   <item><b>Deadlock</b> — threads are blocked (sleeping / waiting) and CPU usage drops
///     to zero for the affected threads.</item>
///   <item><b>Livelock</b> — threads are running (calling <c>Thread.Yield</c> in a hot
///     loop) and CPU usage remains elevated despite zero progress.</item>
/// </list>
/// </para>
/// <para>
/// <b>Fix — randomised back-off:</b> Adding a random delay before each retry breaks the
/// symmetry. The probability that both threads back off for exactly the same duration is
/// negligible, so one of them proceeds while the other waits.
/// </para>
/// </remarks>
public static class LivelockDemo
{
    /// <summary>
    /// Simulates a livelock where two threads repeatedly yield to each other and never
    /// complete their work. Each thread iterates up to <paramref name="maxIterations"/>
    /// times and immediately backs off because the other thread always signals intent at
    /// the same moment (symmetric behaviour).
    /// </summary>
    /// <param name="maxIterations">
    /// Upper bound on the number of back-off iterations before both threads give up.
    /// The method always returns this value, indicating the limit was reached without
    /// completing any useful work (livelock).
    /// </param>
    /// <returns>
    /// <paramref name="maxIterations"/> — the threads exhausted their retry budget without
    /// ever acquiring the resource. Reaching the limit is the observable sign of livelock.
    /// </returns>
    /// <example>
    /// <code>
    /// int iterations = LivelockDemo.Broken(100);
    /// // iterations == 100: both threads gave up after hitting the limit
    /// </code>
    /// </example>
    public static int Broken(int maxIterations)
    {
        // Simulation: both threads signal intent, detect the other's signal,
        // and immediately back off — symmetrically, every iteration.
        // Neither ever acquires the resource; both exhaust their retry budget.
        int wantsResource1 = 1; // Thread 1 always signals intent
        int wantsResource2 = 1; // Thread 2 always signals intent

        Thread thread1 = new Thread(() =>
        {
            for (int i = 0; i < maxIterations; i++)
            {
                Volatile.Write(ref wantsResource1, 1);

                // Thread 2 also wants it — politely back off
                if (Volatile.Read(ref wantsResource2) == 1)
                {
                    Volatile.Write(ref wantsResource1, 0);
                    Thread.Yield(); // give way — but Thread 2 does the same thing
                }
            }
        });

        Thread thread2 = new Thread(() =>
        {
            for (int i = 0; i < maxIterations; i++)
            {
                Volatile.Write(ref wantsResource2, 1);

                // Thread 1 also wants it — politely back off
                if (Volatile.Read(ref wantsResource1) == 1)
                {
                    Volatile.Write(ref wantsResource2, 0);
                    Thread.Yield(); // give way — but Thread 1 does the same thing
                }
            }
        });

        thread1.Start();
        thread2.Start();
        thread1.Join();
        thread2.Join();

        // Both threads exhausted maxIterations without making real progress.
        return maxIterations;
    }

    /// <summary>
    /// Demonstrates that randomised back-off resolves the livelock caused by symmetric
    /// yielding. Each thread waits a random number of milliseconds before retrying,
    /// breaking the symmetry that prevents progress.
    /// </summary>
    /// <param name="iterations">
    /// Total number of work items to complete across both threads. The work is split
    /// evenly: each thread completes half, for a grand total of exactly
    /// <paramref name="iterations"/>.
    /// </param>
    /// <returns>
    /// <paramref name="iterations"/> — all work items were completed successfully.
    /// </returns>
    /// <example>
    /// <code>
    /// int completed = LivelockDemo.Fixed(10);
    /// // completed == 10: randomised back-off broke the symmetry
    /// </code>
    /// </example>
    public static int Fixed(int iterations)
    {
        int completed = 0;
        int half = iterations / 2;
        int rest = iterations - half;

        Thread thread1 = new Thread(() =>
        {
            for (int i = 0; i < half; i++)
            {
                // Randomised back-off: threads are unlikely to retry at the same instant
                Thread.Sleep(Random.Shared.Next(1, 10));
                Interlocked.Increment(ref completed);
            }
        });

        Thread thread2 = new Thread(() =>
        {
            for (int i = 0; i < rest; i++)
            {
                Thread.Sleep(Random.Shared.Next(1, 10));
                Interlocked.Increment(ref completed);
            }
        });

        thread1.Start();
        thread2.Start();
        thread1.Join();
        thread2.Join();

        return completed;
    }
}
