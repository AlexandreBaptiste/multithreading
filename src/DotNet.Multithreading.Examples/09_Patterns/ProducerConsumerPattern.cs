// ============================================================
// Concept  : Producer-Consumer pattern using Channel<T>
// Summary  : Multiple producers write to a bounded channel; multiple
//            consumers read and process items concurrently.
// When to use   : Work queues, log aggregation, download pipelines —
//                 anywhere you need to decouple production rate from
//                 consumption rate and process items concurrently.
// When NOT to use: When throughput is trivially low (just await directly);
//                  for simple sequential processing you gain nothing from a
//                  channel and only add complexity.
// ============================================================

using System.Threading.Channels;

namespace DotNet.Multithreading.Examples.Patterns;

/// <summary>
/// Demonstrates the Producer-Consumer pattern using <c>Channel&lt;T&gt;</c>
/// with multiple producers and multiple consumers running concurrently.
/// </summary>
/// <remarks>
/// <para>
/// Key design decisions:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Bounded channel</b> with capacity <c>producerCount * 2</c> provides
///       natural backpressure: producers suspend (asynchronously) when the channel
///       is full rather than flooding memory.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Graceful shutdown:</b> after all producers finish, <c>Writer.Complete()</c>
///       signals consumers that no more items will arrive. Consumers drain the
///       remaining items and then exit their <c>ReadAllAsync</c> loop automatically.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Thread-safe accumulation:</b> <c>Interlocked.Add</c> is used instead of
///       a lock because we only need atomic addition on a single <c>long</c> field.
///     </description>
///   </item>
/// </list>
/// </remarks>
public static class ProducerConsumerPattern
{
    /// <summary>
    /// Runs a multi-producer multi-consumer pipeline using <c>Channel&lt;T&gt;</c>.
    /// Producers write <paramref name="itemsPerProducer"/> items each.
    /// Consumers read all items and accumulate the sum.
    /// Graceful shutdown: producers complete the channel; consumers drain it.
    /// </summary>
    /// <param name="producerCount">Number of producer tasks.</param>
    /// <param name="consumerCount">Number of consumer tasks.</param>
    /// <param name="itemsPerProducer">Number of items each producer writes.</param>
    /// <param name="ct">Cancellation token for cooperative shutdown.</param>
    /// <returns>Total sum of all consumed items.</returns>
    public static async Task<long> RunAsync(
        int producerCount,
        int consumerCount,
        int itemsPerProducer,
        CancellationToken ct = default)
    {
        Channel<int> channel = Channel.CreateBounded<int>(
            new BoundedChannelOptions(producerCount * 2)
            {
                SingleWriter = false,
                SingleReader = false
            });

        // Launch producers — each writes items 1..itemsPerProducer
        Task[] producers = Enumerable.Range(0, producerCount)
            .Select(_ => ProduceAsync(channel.Writer, itemsPerProducer, ct))
            .ToArray();

        Task producersTask = Task.WhenAll(producers);

        // Close the channel once all producers have finished (success OR cancellation/fault).
        // This ensures consumers always get a completion signal and don't hang forever.
        _ = producersTask.ContinueWith(
            _ => channel.Writer.Complete(),
            TaskContinuationOptions.ExecuteSynchronously);

        // Launch consumers — each drains the channel and adds to a shared long[]
        // (arrays allow Interlocked operations via ref on element 0 without needing
        // a ref parameter, which is forbidden on async methods).
        long[] totals = new long[1];
        Task[] consumers = Enumerable.Range(0, consumerCount)
            .Select(_ => ConsumeAsync(channel.Reader, totals, ct))
            .ToArray();

        // Await both sides so that cancellation/faults from producers propagate to the caller.
        await Task.WhenAll(consumers).ConfigureAwait(false);

        // Re-observe any producer exception (e.g. OperationCanceledException) after consumers finish.
        await producersTask.ConfigureAwait(false);

        return totals[0];
    }

    private static async Task ProduceAsync(
        ChannelWriter<int> writer,
        int itemsPerProducer,
        CancellationToken ct)
    {
        for (int i = 1; i <= itemsPerProducer; i++)
        {
            await writer.WriteAsync(i, ct).ConfigureAwait(false);
        }
    }

    private static async Task ConsumeAsync(
        ChannelReader<int> reader,
        long[] totals,
        CancellationToken ct)
    {
        await foreach (int item in reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            Interlocked.Add(ref totals[0], item);
        }
    }
}
