// ============================================================
// Concept  : System.Threading.Channels.Channel<T>
// Summary  : Async-native, high-performance producer/consumer pipe.
//            Supports unbounded and bounded modes with configurable
//            backpressure strategies: Wait, DropOldest, DropNewest,
//            DropWrite, and Ignore.
// When to use   : All modern async/await producer/consumer workloads.
//                 Prefer over BlockingCollection<T> whenever consumers
//                 are async — channels never block a thread pool thread;
//                 they suspend the logical flow using await.
// When NOT to use: Synchronous thread-based consumers that intentionally
//                  block — use BlockingCollection<T> in those legacy cases.
// ============================================================

using System.Threading.Channels;

namespace DotNet.Multithreading.Examples.ConcurrentCollections;

/// <summary>
/// Demonstrates async producer/consumer pipelines using
/// <see cref="Channel{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why Channel&lt;T&gt; is preferred over <c>BlockingCollection&lt;T&gt;</c>
/// for async code:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Awaitable backpressure:</b> <c>WriteAsync</c> and <c>ReadAsync</c>
///       are truly asynchronous — they yield the thread back to the pool when the
///       channel is full or empty, rather than blocking the OS thread.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Configurable overflow strategies:</b> <see cref="BoundedChannelFullMode"/>
///       lets callers choose between waiting (<c>Wait</c>), dropping the oldest item
///       (<c>DropOldest</c>), dropping the newest (<c>DropNewest</c>), or silently
///       ignoring the write (<c>DropWrite</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Zero-allocation fast path:</b> <c>TryRead</c> / <c>TryWrite</c> succeed
///       synchronously when the channel has capacity, avoiding <c>Task</c>
///       allocations on the hot path.
///     </description>
///   </item>
/// </list>
/// </remarks>
public static class ChannelDemo
{
    /// <summary>
    /// Creates an unbounded channel, writes integers from <c>0</c> to
    /// <paramref name="itemCount"/> − 1, reads them all asynchronously with
    /// <c>ReadAllAsync</c>, and returns their sum.
    /// </summary>
    /// <param name="itemCount">Number of integers to write and read.</param>
    /// <returns>
    /// A task whose result is the sum of all integers read from the channel;
    /// equal to <c>0 + 1 + … + (itemCount − 1)</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// int sum = await ChannelDemo.UnboundedChannelRoundtrip(100);
    /// // sum == 4950
    /// </code>
    /// </example>
    public static async Task<int> UnboundedChannelRoundtrip(int itemCount)
    {
        Channel<int> channel = Channel.CreateUnbounded<int>();

        for (int i = 0; i < itemCount; i++)
        {
            await channel.Writer.WriteAsync(i).ConfigureAwait(false);
        }

        channel.Writer.Complete();

        int sum = 0;

        await foreach (int item in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            sum += item;
        }

        return sum;
    }

    /// <summary>
    /// Creates a bounded channel with <paramref name="capacity"/>, writes
    /// <paramref name="producerItems"/> items from a concurrent producer task
    /// (which awaits backpressure when the channel is full), reads all items,
    /// and returns the count of items read.
    /// </summary>
    /// <param name="capacity">Maximum number of items the channel can buffer.</param>
    /// <param name="producerItems">Total items the producer will write.</param>
    /// <returns>
    /// A task whose result is the count of items read; always equals
    /// <paramref name="producerItems"/> because <c>WriteAsync</c> awaits
    /// backpressure rather than dropping items.
    /// </returns>
    /// <example>
    /// <code>
    /// int count = await ChannelDemo.BoundedChannelBackpressure(5, 20);
    /// // count == 20
    /// </code>
    /// </example>
    public static async Task<int> BoundedChannelBackpressure(int capacity, int producerItems)
    {
        Channel<int> channel = Channel.CreateBounded<int>(capacity);

        // Producer runs concurrently; it will await when the channel is full.
        Task writeTask = Task.Run(async () =>
        {
            for (int i = 0; i < producerItems; i++)
            {
                await channel.Writer.WriteAsync(i).ConfigureAwait(false);
            }

            channel.Writer.Complete();
        });

        int count = 0;

        await foreach (int item in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            count++;
        }

        await writeTask.ConfigureAwait(false);

        return count;
    }

    /// <summary>
    /// Creates a bounded channel configured with
    /// <see cref="BoundedChannelFullMode.DropOldest"/>, writes
    /// <paramref name="capacity"/> + 2 items synchronously via <c>TryWrite</c>,
    /// then reads all remaining items and returns them.
    /// </summary>
    /// <param name="capacity">Maximum channel capacity.</param>
    /// <returns>
    /// A task whose result is a list containing the <paramref name="capacity"/>
    /// newest items; the two oldest items written are dropped as the channel
    /// overflows.
    /// </returns>
    /// <remarks>
    /// With <c>DropOldest</c>, every write beyond capacity atomically removes the
    /// head of the internal queue (the oldest pending item) before inserting the
    /// new one, so the channel always retains the most recently written items.
    /// </remarks>
    /// <example>
    /// <code>
    /// // capacity == 3: writes 0,1,2 → full; write 3 drops 0 → [1,2,3];
    /// //                write 4 drops 1 → [2,3,4]
    /// List&lt;int&gt; items = await ChannelDemo.BoundedChannelDropOldest(3);
    /// // items == [2, 3, 4]
    /// </code>
    /// </example>
    public static async Task<List<int>> BoundedChannelDropOldest(int capacity)
    {
        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        };

        Channel<int> channel = Channel.CreateBounded<int>(options);

        // Write capacity + 2 items synchronously; the two oldest will be dropped.
        for (int i = 0; i < capacity + 2; i++)
        {
            channel.Writer.TryWrite(i);
        }

        channel.Writer.Complete();

        List<int> result = new(capacity);

        await foreach (int item in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            result.Add(item);
        }

        return result;
    }
}
