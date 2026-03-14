// ============================================================
// Concept  : SpinLock, SpinWait
// Summary  : Demonstrates SpinLock and SpinWait for low-latency synchronisation in short critical sections on multi-core machines.
// When to use   : VERY short critical sections on multi-core machines where blocking overhead (kernel transition) exceeds the wait time.
// When NOT to use: Single-core machines, I/O-bound work, or waits longer than a few microseconds. SpinLock wastes CPU.
// ============================================================

namespace DotNet.Multithreading.Examples.Synchronization;

/// <summary>
/// Demonstrates <see cref="SpinLock"/> and <see cref="SpinWait"/> for low-latency
/// synchronisation in short critical sections on multi-core machines.
/// </summary>
public static class SpinDemo
{
    /// <summary>
    /// Increments a shared counter from <paramref name="threadCount"/> concurrent threads using
    /// a <see cref="SpinLock"/> for mutual exclusion.
    /// </summary>
    /// <remarks>
    /// <see cref="SpinLock"/> is a value type (struct). It must never be copied — storing it as a
    /// field in a helper class ensures the same instance is always used by reference.
    /// </remarks>
    /// <param name="threadCount">Number of threads that each increment the counter once.</param>
    /// <returns>Final counter value; equals <paramref name="threadCount"/>.</returns>
    /// <example>
    /// <code>
    /// int result = SpinDemo.SpinLockExample(10);
    /// // result == 10
    /// </code>
    /// </example>
    public static int SpinLockExample(int threadCount)
    {
        // SpinLock is a struct — it must be stored by ref, NEVER copied.
        // Declare as a field to avoid accidental copy.
        SpinLockCounter helper = new SpinLockCounter();
        return helper.Run(threadCount);
    }

    /// <summary>
    /// Demonstrates <see cref="SpinWait.SpinUntil(Func{bool}, int)"/> by waiting for a flag
    /// that is set from another thread after a short delay.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the signal was received within the 2-second timeout;
    /// <see langword="false"/> on timeout.
    /// </returns>
    /// <example>
    /// <code>
    /// bool received = SpinDemo.SpinWaitExample();
    /// // received == true
    /// </code>
    /// </example>
    public static bool SpinWaitExample()
    {
        bool flag = false;

        Thread signaller = new Thread(() =>
        {
            Thread.Sleep(50);
            Volatile.Write(ref flag, true);
        });
        signaller.IsBackground = true;
        signaller.Start();

        bool result = SpinWait.SpinUntil(() => Volatile.Read(ref flag), 2000);

        signaller.Join();

        return result;
    }

    // -------------------------------------------------------------------------
    // Private helper — keeps SpinLock as a struct field, never copied.
    // -------------------------------------------------------------------------

    private sealed class SpinLockCounter
    {
        // enableThreadOwnerTracking: false for production paths to avoid overhead.
        private SpinLock _spinLock = new SpinLock(enableThreadOwnerTracking: false);
        private int _counter;

        /// <summary>
        /// Runs <paramref name="threadCount"/> threads, each acquiring the spin lock and
        /// incrementing the internal counter once.
        /// </summary>
        public int Run(int threadCount)
        {
            Thread[] threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    bool lockTaken = false;
                    try
                    {
                        _spinLock.Enter(ref lockTaken);
                        _counter++;
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            _spinLock.Exit();
                        }
                    }
                });
                threads[i].Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            return _counter;
        }
    }
}
