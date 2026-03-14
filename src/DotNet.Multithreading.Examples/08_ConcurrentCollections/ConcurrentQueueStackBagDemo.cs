// ============================================================
// Concept  : ConcurrentQueue<T>, ConcurrentStack<T>, ConcurrentBag<T>
// Summary  : Lock-free thread-safe collections for producer/consumer
//            and work-stealing scenarios. Each has distinct ordering
//            semantics suited to different algorithm requirements.
// When to use   : Choose based on ordering needs: FIFO (Queue),
//                 LIFO (Stack), or unordered with work-stealing (Bag).
// When NOT to use: Do not use ConcurrentBag when producers and consumers
//                  are always different threads — it loses its work-stealing
//                  advantage and performs worse than ConcurrentQueue.
// ============================================================

using System.Collections.Concurrent;

namespace DotNet.Multithreading.Examples.ConcurrentCollections;

/// <summary>
/// Demonstrates <see cref="ConcurrentQueue{T}"/>, <see cref="ConcurrentStack{T}"/>,
/// and <see cref="ConcurrentBag{T}"/> with their respective ordering semantics.
/// </summary>
/// <remarks>
/// <para>
/// <b>Collection selection guide:</b>
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Collection</term>
///     <description>Ordering / Best use case</description>
///   </listheader>
///   <item>
///     <term><see cref="ConcurrentQueue{T}"/></term>
///     <description>
///       <b>FIFO.</b> Classic producer/consumer pipelines where processing
///       order matters and producer threads differ from consumer threads.
///     </description>
///   </item>
///   <item>
///     <term><see cref="ConcurrentStack{T}"/></term>
///     <description>
///       <b>LIFO.</b> Undo/redo history, depth-first traversal queues, or
///       scenarios where the most recently added item must be processed first.
///     </description>
///   </item>
///   <item>
///     <term><see cref="ConcurrentBag{T}"/></term>
///     <description>
///       <b>Unordered.</b> Optimised for work-stealing when the same thread
///       both produces and consumes items (e.g., thread-local object pools).
///       Performs worse than <see cref="ConcurrentQueue{T}"/> when producers
///       and consumers are always different threads.
///     </description>
///   </item>
/// </list>
/// </remarks>
public static class ConcurrentQueueStackBagDemo
{
    /// <summary>
    /// Enqueues integers from <c>0</c> to <paramref name="itemCount"/> − 1 into a
    /// <see cref="ConcurrentQueue{T}"/> and dequeues them all, returning the items
    /// in dequeue order.
    /// </summary>
    /// <param name="itemCount">Number of items to enqueue and dequeue.</param>
    /// <returns>
    /// Items in FIFO order — identical to enqueue order in single-threaded usage.
    /// </returns>
    /// <example>
    /// <code>
    /// List&lt;int&gt; items = ConcurrentQueueStackBagDemo.QueueFifoOrder(5);
    /// // items == [0, 1, 2, 3, 4]
    /// </code>
    /// </example>
    public static List<int> QueueFifoOrder(int itemCount)
    {
        ConcurrentQueue<int> queue = new();

        for (int i = 0; i < itemCount; i++)
        {
            queue.Enqueue(i);
        }

        List<int> result = new(itemCount);

        while (queue.TryDequeue(out int item))
        {
            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Pushes integers from <c>0</c> to <paramref name="itemCount"/> − 1 onto a
    /// <see cref="ConcurrentStack{T}"/> and pops them all, returning the items in
    /// pop order.
    /// </summary>
    /// <param name="itemCount">Number of items to push and pop.</param>
    /// <returns>
    /// Items in LIFO order — the reverse of push order in single-threaded usage.
    /// </returns>
    /// <example>
    /// <code>
    /// List&lt;int&gt; items = ConcurrentQueueStackBagDemo.StackLifoOrder(5);
    /// // items == [4, 3, 2, 1, 0]
    /// </code>
    /// </example>
    public static List<int> StackLifoOrder(int itemCount)
    {
        ConcurrentStack<int> stack = new();

        for (int i = 0; i < itemCount; i++)
        {
            stack.Push(i);
        }

        List<int> result = new(itemCount);

        while (stack.TryPop(out int item))
        {
            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Spawns <paramref name="producerCount"/> threads, each adding
    /// <paramref name="itemsPerProducer"/> items to a shared
    /// <see cref="ConcurrentBag{T}"/>, and returns the total item count.
    /// </summary>
    /// <param name="producerCount">Number of concurrent producer threads.</param>
    /// <param name="itemsPerProducer">Number of items each producer thread adds.</param>
    /// <returns>
    /// Total items in the bag; always equals
    /// <paramref name="producerCount"/> × <paramref name="itemsPerProducer"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int total = ConcurrentQueueStackBagDemo.BagMultipleProducers(5, 10);
    /// // total == 50
    /// </code>
    /// </example>
    public static int BagMultipleProducers(int producerCount, int itemsPerProducer)
    {
        ConcurrentBag<int> bag = new();
        Thread[] threads = new Thread[producerCount];

        for (int i = 0; i < producerCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < itemsPerProducer; j++)
                {
                    bag.Add(j);
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

        return bag.Count;
    }
}
