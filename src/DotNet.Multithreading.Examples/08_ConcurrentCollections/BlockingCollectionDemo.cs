// ============================================================
// Concept  : BlockingCollection<T>
// Summary  : A thread-safe bounded/unbounded wrapper around any
//            IProducerConsumerCollection<T> (default: ConcurrentQueue<T>)
//            that supports blocking Add/Take and graceful shutdown via
//            CompleteAdding(). The producer blocks when the collection is
//            full; the consumer blocks when it is empty.
// When to use   : Classic producer/consumer pipelines with synchronous,
//                 dedicated-thread consumers (TPL Dataflow, background
//                 worker threads, legacy synchronous pipelines).
// When NOT to use: async consumer code — blocking Take() wastes a
//                  thread-pool thread. Use Channel<T> instead when
//                  consumers are async.
// ============================================================

using System.Collections.Concurrent;

namespace DotNet.Multithreading.Examples.ConcurrentCollections;

/// <summary>
/// Demonstrates bounded producer/consumer pipelines using
/// <see cref="BlockingCollection{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>BlockingCollection vs Channel&lt;T&gt;:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b><see cref="BlockingCollection{T}"/>:</b> Synchronous blocking
///       semantics. <c>Add</c> blocks the producer thread when the collection
///       is full; <c>Take</c> blocks the consumer thread when it is empty.
///       Ideal for dedicated background threads in legacy or TPL-based code.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Channel&lt;T&gt;:</b> Async-native. <c>WriteAsync</c> and
///       <c>ReadAsync</c> are truly awaitable — they suspend the logical
///       flow without blocking an OS thread. Preferred for all modern
///       <c>async/await</c> producer/consumer workloads.
///     </description>
///   </item>
/// </list>
/// </remarks>
public static class BlockingCollectionDemo
{
    /// <summary>
    /// Starts a producer thread that adds <paramref name="itemCount"/> integers to a
    /// bounded <see cref="BlockingCollection{T}"/> and a consumer thread that drains
    /// it via <c>GetConsumingEnumerable</c>, returning the sum of all consumed values.
    /// </summary>
    /// <param name="itemCount">Number of items the producer enqueues (0 … itemCount−1).</param>
    /// <param name="boundedCapacity">Maximum capacity of the collection.</param>
    /// <returns>
    /// Sum of all consumed integers, equal to <c>0 + 1 + … + (itemCount − 1)</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// int sum = BlockingCollectionDemo.ProducerConsumer(100, 10);
    /// // sum == 4950  (0+1+…+99)
    /// </code>
    /// </example>
    public static int ProducerConsumer(int itemCount, int boundedCapacity)
    {
        using BlockingCollection<int> collection = new(boundedCapacity);

        int totalSum = 0;

        Thread producer = new(() =>
        {
            for (int i = 0; i < itemCount; i++)
            {
                collection.Add(i);
            }

            collection.CompleteAdding();
        });

        Thread consumer = new(() =>
        {
            foreach (int item in collection.GetConsumingEnumerable())
            {
                Interlocked.Add(ref totalSum, item);
            }
        });

        producer.Start();
        consumer.Start();

        producer.Join();
        consumer.Join();

        return totalSum;
    }

    /// <summary>
    /// Attempts to take an item from an empty <see cref="BlockingCollection{T}"/>
    /// within the specified timeout.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds to wait for an item.</param>
    /// <returns>
    /// <see langword="false"/> because the collection is empty and the
    /// timeout expires before any item becomes available.
    /// </returns>
    /// <example>
    /// <code>
    /// bool taken = BlockingCollectionDemo.TryTakeWithTimeout(50);
    /// // taken == false
    /// </code>
    /// </example>
    public static bool TryTakeWithTimeout(int timeoutMs)
    {
        using BlockingCollection<int> collection = new();

        return collection.TryTake(out _, timeoutMs);
    }

    /// <summary>
    /// Fills a bounded <see cref="BlockingCollection{T}"/> to capacity, then attempts
    /// to add one more item from a separate thread within a 50 ms timeout.
    /// </summary>
    /// <param name="capacity">Maximum capacity of the collection.</param>
    /// <returns>
    /// <see langword="false"/> because the collection is full and the timed add
    /// expires before a slot becomes available.
    /// </returns>
    /// <example>
    /// <code>
    /// bool added = BlockingCollectionDemo.BoundedCollectionBlocksProducer(5);
    /// // added == false
    /// </code>
    /// </example>
    public static bool BoundedCollectionBlocksProducer(int capacity)
    {
        using BlockingCollection<int> collection = new(capacity);

        // Fill the collection to its capacity on the current thread.
        for (int i = 0; i < capacity; i++)
        {
            collection.Add(i);
        }

        // Attempt to add one more item from another thread; the collection is full
        // so TryAdd will block until the timeout elapses and return false.
        bool added = false;

        Thread producer = new(() =>
        {
            added = collection.TryAdd(99, 50);
        });

        producer.Start();
        producer.Join();

        return added;
    }
}
