// ============================================================
// Concept  : ManualResetEventSlim, AutoResetEvent, CountdownEvent
// Summary  : Demonstrates thread signalling primitives for broadcast, one-shot gate, and composite signal scenarios.
// When to use   : Signalling between threads. ManualResetEvent=broadcast, AutoResetEvent=one-shot gate, CountdownEvent=composite signal.
// When NOT to use: Prefer Task-based signalling (TaskCompletionSource) in async code.
// ============================================================

namespace DotNet.Multithreading.Examples.Synchronization;

/// <summary>
/// Demonstrates thread signalling primitives: <see cref="ManualResetEventSlim"/>,
/// <see cref="AutoResetEvent"/>, and <see cref="CountdownEvent"/>.
/// </summary>
public static class EventWaitHandleDemo
{
    /// <summary>
    /// Creates a <see cref="ManualResetEventSlim"/> that is initially unset, then signals it
    /// so that all waiting threads are unblocked simultaneously (broadcast semantics).
    /// </summary>
    /// <param name="waiterCount">Number of threads to start and unblock.</param>
    /// <returns>
    /// The number of threads that successfully unblocked; equals <paramref name="waiterCount"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int unblocked = EventWaitHandleDemo.ManualResetEventUnblocksAll(5);
    /// // unblocked == 5
    /// </code>
    /// </example>
    public static int ManualResetEventUnblocksAll(int waiterCount)
    {
        using ManualResetEventSlim mre = new ManualResetEventSlim(false);
        using CountdownEvent finished = new CountdownEvent(waiterCount);

        int counter = 0;

        Thread[] threads = new Thread[waiterCount];
        for (int i = 0; i < waiterCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                mre.Wait();
                Interlocked.Increment(ref counter);
                finished.Signal();
            });
            threads[i].IsBackground = true;
            threads[i].Start();
        }

        // Give threads time to reach Wait() before signalling.
        Thread.Sleep(20);
        mre.Set();

        finished.Wait(TimeSpan.FromSeconds(5));

        return counter;
    }

    /// <summary>
    /// Creates an <see cref="AutoResetEvent"/> (initially unset) and starts
    /// <paramref name="attemptCount"/> threads each racing to receive the single available signal.
    /// Only one thread wins because <see cref="AutoResetEvent"/> resets automatically after
    /// releasing a single waiter.
    /// </summary>
    /// <param name="attemptCount">Number of competing threads.</param>
    /// <returns>
    /// The number of threads that received the signal; should be exactly 1.
    /// </returns>
    /// <example>
    /// <code>
    /// int winners = EventWaitHandleDemo.AutoResetEventUnblocksOne(5);
    /// // winners == 1
    /// </code>
    /// </example>
    public static int AutoResetEventUnblocksOne(int attemptCount)
    {
        using AutoResetEvent are = new AutoResetEvent(false);
        // Counts down once each thread is ready to call WaitOne.
        using CountdownEvent allReady = new CountdownEvent(attemptCount);
        using CountdownEvent allDone = new CountdownEvent(attemptCount);

        int successCount = 0;

        Thread[] threads = new Thread[attemptCount];
        for (int i = 0; i < attemptCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                allReady.Signal();
                // Small spin to ensure all threads have actually reached WaitOne.
                SpinWait.SpinUntil(() => allReady.IsSet);

                bool received = are.WaitOne(200);
                if (received)
                {
                    Interlocked.Increment(ref successCount);
                }

                allDone.Signal();
            });
            threads[i].IsBackground = true;
            threads[i].Start();
        }

        // Wait until all threads are ready, then allow a small margin before signalling.
        allReady.Wait(TimeSpan.FromSeconds(5));
        Thread.Sleep(10);
        are.Set();

        allDone.Wait(TimeSpan.FromSeconds(5));

        return successCount;
    }

    /// <summary>
    /// Demonstrates <see cref="CountdownEvent"/> as a composite completion signal.
    /// Spawns <paramref name="participantCount"/> threads each calling
    /// <see cref="CountdownEvent.Signal()"/>; the main thread waits until the count reaches zero.
    /// </summary>
    /// <param name="participantCount">Number of participant threads.</param>
    /// <returns>
    /// <see langword="true"/> if the countdown reached zero within 5 seconds;
    /// <see langword="false"/> on timeout.
    /// </returns>
    /// <example>
    /// <code>
    /// bool done = EventWaitHandleDemo.CountdownEventSignalsWhenDone(5);
    /// // done == true
    /// </code>
    /// </example>
    public static bool CountdownEventSignalsWhenDone(int participantCount)
    {
        using CountdownEvent cde = new CountdownEvent(participantCount);

        Thread[] threads = new Thread[participantCount];
        for (int i = 0; i < participantCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                // Simulate some work.
                Thread.SpinWait(1000);
                cde.Signal();
            });
            threads[i].IsBackground = true;
            threads[i].Start();
        }

        return cde.Wait(TimeSpan.FromSeconds(5));
    }
}
