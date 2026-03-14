// ============================================================
// Concept  : ConcurrentDictionary<TKey, TValue>
// Summary  : Thread-safe dictionary supporting lock-free reads and
//            fine-grained stripe locking for writes. Ideal for shared
//            caches, frequency counters, and concurrent registries.
// When to use   : Multiple threads reading and writing to a shared
//                 dictionary without external locking.
// When NOT to use: Single-threaded access (adds unnecessary overhead);
//                  compound operations that must be atomic across multiple
//                  dictionary API calls — coordinate with an external lock
//                  in those cases.
// ============================================================

using System.Collections.Concurrent;

namespace DotNet.Multithreading.Examples.ConcurrentCollections;

/// <summary>
/// Demonstrates thread-safe operations on <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>⚠ GetOrAdd factory-call warning:</b>
/// <c>GetOrAdd(key, valueFactory)</c> does <i>not</i> guarantee that the factory
/// delegate is invoked exactly once under contention. Two threads may race, both
/// evaluate the factory, and only one result is stored; the other is silently
/// discarded. If the factory has observable side-effects — such as opening a
/// database connection, allocating a network socket, or writing to a file —
/// concurrent evaluation can cause resource leaks or duplicate operations.
/// </para>
/// <para>
/// <b>Safe alternative:</b> Pre-compute the value before the call and use
/// <c>GetOrAdd(key, precomputedValue)</c>, or wrap the expensive work in a
/// <see cref="Lazy{T}"/> so that it is deferred and performed at most once
/// regardless of how many threads race.
/// </para>
/// <para>
/// <b>AddOrUpdate atomicity:</b> Each individual <c>AddOrUpdate</c> call is
/// atomic per-call. The update delegate may be retried internally when CAS
/// fails under high contention, but every logical increment is counted
/// exactly once.
/// </para>
/// </remarks>
public static class ConcurrentDictionaryDemo
{
    /// <summary>
    /// Spawns <paramref name="threadCount"/> threads that all call
    /// <c>GetOrAdd</c> for the same key concurrently and returns the
    /// stored value.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads to launch.</param>
    /// <returns>
    /// The value associated with the shared key after all threads complete;
    /// always <c>1</c> regardless of concurrency level.
    /// </returns>
    /// <remarks>
    /// Even though multiple threads may invoke the factory concurrently,
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/> ensures only one value
    /// is ever stored for the key — the losers' results are discarded.
    /// </remarks>
    /// <example>
    /// <code>
    /// int value = ConcurrentDictionaryDemo.GetOrAddConcurrently(100);
    /// // value == 1
    /// </code>
    /// </example>
    public static int GetOrAddConcurrently(int threadCount)
    {
        ConcurrentDictionary<string, int> dict = new();
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                dict.GetOrAdd("shared-key", _ => 1);
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

        return dict["shared-key"];
    }

    /// <summary>
    /// Spawns <paramref name="threadCount"/> threads, each calling
    /// <c>AddOrUpdate</c> exactly <paramref name="addsPerThread"/> times,
    /// incrementing a shared counter by one on every call.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads to launch.</param>
    /// <param name="addsPerThread">Number of increment operations per thread.</param>
    /// <returns>
    /// Final counter value; always equals
    /// <paramref name="threadCount"/> × <paramref name="addsPerThread"/>.
    /// </returns>
    /// <remarks>
    /// Each <c>AddOrUpdate</c> call is atomic. The update delegate
    /// <c>(_, old) =&gt; old + 1</c> may be retried internally under high
    /// contention because <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// uses a compare-and-swap loop, but every logical increment is counted
    /// exactly once — no increments are lost.
    /// </remarks>
    /// <example>
    /// <code>
    /// int total = ConcurrentDictionaryDemo.AddOrUpdateAccumulator(10, 10);
    /// // total == 100
    /// </code>
    /// </example>
    public static int AddOrUpdateAccumulator(int threadCount, int addsPerThread)
    {
        ConcurrentDictionary<string, int> dict = new();
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < addsPerThread; j++)
                {
                    dict.AddOrUpdate("counter", 1, (_, old) => old + 1);
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

        return dict["counter"];
    }

    /// <summary>
    /// Adds <paramref name="itemCount"/> entries using <c>TryAdd</c> and
    /// then removes them all using <c>TryRemove</c>, returning the count of
    /// successfully removed items.
    /// </summary>
    /// <param name="itemCount">Number of items to add and subsequently remove.</param>
    /// <returns>
    /// Count of successfully removed items; equals <paramref name="itemCount"/>
    /// because every added key is unique and present at removal time.
    /// </returns>
    /// <example>
    /// <code>
    /// int removed = ConcurrentDictionaryDemo.TryAddTryRemoveConcurrently(100);
    /// // removed == 100
    /// </code>
    /// </example>
    public static int TryAddTryRemoveConcurrently(int itemCount)
    {
        ConcurrentDictionary<int, int> dict = new();

        Parallel.For(0, itemCount, i => dict.TryAdd(i, i));

        int removed = 0;

        Parallel.For(0, itemCount, i =>
        {
            if (dict.TryRemove(i, out _))
            {
                Interlocked.Increment(ref removed);
            }
        });

        return removed;
    }
}
