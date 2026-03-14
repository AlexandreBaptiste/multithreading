// ============================================================
// Concept  : Interlocked (Atomic Operations)
// Summary  : Provides atomic read-modify-write operations (Increment, Add, Exchange, CompareExchange) that are safe from multiple threads without a lock
// When to use   : When you need a single atomic read-modify-write on ONE variable
// When NOT to use: When you must protect an invariant spanning multiple variables or the operation is not expressible as a single CAS/exchange — use lock instead
// ============================================================

using System.Threading;

namespace DotNet.Multithreading.Examples.AtomicOperations;

/// <summary>
/// Demonstrates atomic operations provided by <c>System.Threading.Interlocked</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every method on <c>Interlocked</c> is a single atomic read-modify-write instruction,
/// making it impossible for another thread to observe a partially-updated value.
/// </para>
/// <para>
/// <b>Compare-and-Swap (CAS) loop:</b> <c>CompareExchange</c> atomically checks whether
/// a variable still holds an expected value and, only if it does, replaces it with a new
/// value — returning the old value in both cases. A spin loop around CAS implements
/// lock-free mutation: read → compute → CAS; retry if someone else changed the value first.
/// </para>
/// <para>
/// <b>Interlocked vs lock:</b> Prefer <c>Interlocked</c> for a single-variable arithmetic
/// update (counter, flag). Prefer <c>lock</c> when an invariant spans multiple variables
/// or when the operation cannot be expressed as one CAS.
/// </para>
/// </remarks>
public static class InterlockedDemo
{
    // Private state fields reset before each method so tests stay independent.
    private static int _counter;
    private static long _longCounter;
    private static int _exchangeField;

    /// <summary>
    /// Spawns <paramref name="threadCount"/> threads, each calling
    /// <c>Interlocked.Increment</c> <paramref name="incrementsPerThread"/> times on a
    /// shared counter, then returns the final value.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads to spawn.</param>
    /// <param name="incrementsPerThread">How many increments each thread performs.</param>
    /// <returns>
    /// The final counter value, which must equal
    /// <c>threadCount × incrementsPerThread</c> because every increment is atomic.
    /// </returns>
    /// <example>
    /// <code>
    /// int total = InterlockedDemo.IncrementConcurrently(10, 1000);
    /// // total == 10_000
    /// </code>
    /// </example>
    public static int IncrementConcurrently(int threadCount, int incrementsPerThread)
    {
        Interlocked.Exchange(ref _counter, 0);

        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < incrementsPerThread; j++)
                {
                    Interlocked.Increment(ref _counter);
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

        return _counter;
    }

    /// <summary>
    /// Starts <paramref name="startValue"/> threads each calling
    /// <c>Interlocked.Decrement</c> once on a shared counter initialised to
    /// <paramref name="startValue"/>, then returns the final value.
    /// </summary>
    /// <param name="startValue">Initial counter value; also the number of threads.</param>
    /// <returns>
    /// The final counter value — proves atomic decrement is safe (result is 0).
    /// </returns>
    /// <example>
    /// <code>
    /// int result = InterlockedDemo.DecrementToZero(100);
    /// // result == 0
    /// </code>
    /// </example>
    public static int DecrementToZero(int startValue)
    {
        Interlocked.Exchange(ref _counter, startValue);

        Thread[] threads = new Thread[startValue];

        for (int i = 0; i < startValue; i++)
        {
            threads[i] = new Thread(() => Interlocked.Decrement(ref _counter));
        }

        foreach (Thread t in threads)
        {
            t.Start();
        }

        foreach (Thread t in threads)
        {
            t.Join();
        }

        return _counter;
    }

    /// <summary>
    /// Spawns <paramref name="threadCount"/> threads each adding
    /// <paramref name="addAmount"/> to a shared <c>long</c> counter using
    /// <c>Interlocked.Add</c>, then returns the total.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads.</param>
    /// <param name="addAmount">Amount each thread adds.</param>
    /// <returns>
    /// Final counter value equal to <c>threadCount × addAmount</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// long total = InterlockedDemo.AddConcurrently(5, 200);
    /// // total == 1000
    /// </code>
    /// </example>
    public static long AddConcurrently(int threadCount, int addAmount)
    {
        Interlocked.Exchange(ref _longCounter, 0L);

        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() => Interlocked.Add(ref _longCounter, addAmount));
        }

        foreach (Thread t in threads)
        {
            t.Start();
        }

        foreach (Thread t in threads)
        {
            t.Join();
        }

        return _longCounter;
    }

    /// <summary>
    /// Sets a shared field to <paramref name="initial"/>, then atomically exchanges it
    /// for <paramref name="newValue"/> using <c>Interlocked.Exchange</c>.
    /// </summary>
    /// <param name="initial">The value to seed the shared field with.</param>
    /// <param name="newValue">The value to exchange in.</param>
    /// <returns>
    /// The <em>old</em> value that was in the field before the exchange — i.e.,
    /// <paramref name="initial"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int old = InterlockedDemo.ExchangeExample(5, 99);
    /// // old == 5  (field now holds 99)
    /// </code>
    /// </example>
    public static int ExchangeExample(int initial, int newValue)
    {
        _exchangeField = initial;

        int oldValue = Interlocked.Exchange(ref _exchangeField, newValue);

        return oldValue;
    }

    /// <summary>
    /// Demonstrates <c>Interlocked.CompareExchange</c>: atomically replaces the shared
    /// field with <paramref name="newValue"/> only if its current value equals
    /// <paramref name="comparand"/>.
    /// </summary>
    /// <param name="initial">Seed value for the shared field.</param>
    /// <param name="comparand">Value to compare against the current field value.</param>
    /// <param name="newValue">Value to write if the comparison succeeds.</param>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><c>result</c> — the field value <em>after</em> the operation.</item>
    ///   <item><c>observedOld</c> — the value returned by <c>CompareExchange</c>, i.e. the old value.</item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// var (result, old) = InterlockedDemo.CompareExchangeExample(10, 10, 99);
    /// // old == 10, result == 99  (swap succeeded)
    ///
    /// var (result2, old2) = InterlockedDemo.CompareExchangeExample(10, 5, 99);
    /// // old2 == 10, result2 == 10  (swap did NOT occur)
    /// </code>
    /// </example>
    public static (int result, int observedOld) CompareExchangeExample(
        int initial, int comparand, int newValue)
    {
        _exchangeField = initial;

        int observedOld = Interlocked.CompareExchange(ref _exchangeField, newValue, comparand);

        return (_exchangeField, observedOld);
    }

    /// <summary>
    /// Demonstrates the lock-free CAS-loop pattern by incrementing a shared counter
    /// from <paramref name="threadCount"/> threads × <paramref name="incrementsPerThread"/>
    /// times each, using <c>Interlocked.CompareExchange</c> instead of
    /// <c>Interlocked.Increment</c>.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads.</param>
    /// <param name="incrementsPerThread">Increments per thread.</param>
    /// <returns>
    /// Final counter value equal to <c>threadCount × incrementsPerThread</c>.
    /// </returns>
    /// <remarks>
    /// The CAS loop reads the current value, computes the desired next value, then
    /// atomically swaps only if the field has not changed since the read. If another
    /// thread snuck in an update, the loop retries from the new current value.
    /// </remarks>
    /// <example>
    /// <code>
    /// int total = InterlockedDemo.LockFreeCounterCasLoop(10, 100);
    /// // total == 1000
    /// </code>
    /// </example>
    public static int LockFreeCounterCasLoop(int threadCount, int incrementsPerThread)
    {
        Interlocked.Exchange(ref _counter, 0);

        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < incrementsPerThread; j++)
                {
                    int current, updated;

                    do
                    {
                        current = _counter;
                        updated = current + 1;
                    }
                    while (Interlocked.CompareExchange(ref _counter, updated, current) != current);
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

        return _counter;
    }
}
