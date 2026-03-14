// ============================================================
// Concept  : Race Condition
// Summary  : Non-deterministic bugs arising when two or more threads access shared data without synchronization (e.g., lost increments on read-modify-write)
// When to use   : As a learning example — observe in RELEASE mode where the compiler elides memory barriers
// When NOT to use: In production code — always use Interlocked, lock, or other synchronization primitives on all shared mutable state
// ============================================================

using System.Threading;

namespace DotNet.Multithreading.Examples.Pitfalls;

/// <summary>
/// Demonstrates race conditions — a class of concurrency bugs that arise when two or more
/// threads access shared mutable state without synchronization.
/// </summary>
/// <remarks>
/// <para>
/// A race condition occurs when the correctness of a program depends on the relative
/// interleaving of operations in different threads. The non-determinism is caused by the
/// operating system scheduler and by CPU/JIT optimizations that reorder memory operations.
/// </para>
/// <para>
/// <b>Root cause of counter++:</b> The expression compiles to three separate machine
/// instructions — LOAD (read the value), ADD 1, STORE (write back). If two threads execute
/// these instructions concurrently, one store can overwrite the other, silently losing an
/// increment. This is called a "lost update".
/// </para>
/// <para>
/// <b>Fix:</b> <c>Interlocked.Increment</c> emits a single CPU instruction with a LOCK
/// prefix (x86/x64) that makes the entire read-modify-write indivisible.
/// </para>
/// </remarks>
public static class RaceConditionDemo
{
    /// <summary>
    /// Increments a shared counter from multiple threads WITHOUT synchronization.
    /// This demonstrates a race condition — the result is non-deterministic and will
    /// likely be less than <paramref name="threadCount"/> × <paramref name="incrementsPerThread"/>
    /// on a multi-core machine due to lost updates.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads to spawn.</param>
    /// <param name="incrementsPerThread">Number of increments each thread performs.</param>
    /// <returns>
    /// The final counter value. On a multi-core machine this is typically less than
    /// <paramref name="threadCount"/> × <paramref name="incrementsPerThread"/> because
    /// concurrent read-modify-write operations overwrite each other's results.
    /// </returns>
    /// <example>
    /// <code>
    /// int result = RaceConditionDemo.Broken(10, 1000);
    /// // result is probably less than 10_000 — some increments were lost
    /// </code>
    /// </example>
    public static int Broken(int threadCount, int incrementsPerThread)
    {
        int counter = 0;
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < incrementsPerThread; j++)
                    counter++; // NOT atomic: read-modify-write is three instructions
            });
            threads[i].Start();
        }

        foreach (Thread t in threads)
            t.Join();

        return counter;
    }

    /// <summary>
    /// Same counter increment as <c>Broken</c> but uses <c>Interlocked.Increment</c>
    /// for an atomic read-modify-write. The result is always exactly
    /// <paramref name="threadCount"/> × <paramref name="incrementsPerThread"/>,
    /// regardless of thread interleaving.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads to spawn.</param>
    /// <param name="incrementsPerThread">Number of increments each thread performs.</param>
    /// <returns>
    /// The final counter value, which is always exactly
    /// <paramref name="threadCount"/> × <paramref name="incrementsPerThread"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int result = RaceConditionDemo.Fixed(10, 1000);
    /// // result is always exactly 10_000
    /// </code>
    /// </example>
    public static int Fixed(int threadCount, int incrementsPerThread)
    {
        int counter = 0;
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < incrementsPerThread; j++)
                    Interlocked.Increment(ref counter); // Atomic: guaranteed correct result
            });
            threads[i].Start();
        }

        foreach (Thread t in threads)
            t.Join();

        return counter;
    }
}
