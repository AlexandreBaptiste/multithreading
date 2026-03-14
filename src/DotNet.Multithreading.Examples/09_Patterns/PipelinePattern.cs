// ============================================================
// Concept  : Three-stage processing pipeline using chained Channel<T>
// Summary  : Each stage runs as an independent async Task connected by
//            typed channels. Stage1 → Channel<int> → Stage2 → Channel<string>
//            → Stage3 collects results.
// When to use   : ETL workloads, image/video processing chains, multi-step
//                 data transformation where each stage has different
//                 concurrency or resource requirements.
// When NOT to use: Simple sequential transforms — LINQ Select is cleaner.
// ============================================================

using System.Threading.Channels;

namespace DotNet.Multithreading.Examples.Patterns;

/// <summary>
/// Demonstrates a three-stage processing pipeline built from chained
/// <c>Channel&lt;T&gt;</c> instances, where each stage runs as an independent
/// asynchronous task.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline topology:
/// </para>
/// <code>
/// Stage1 (produce ints) ──▶ channel1 ──▶ Stage2 (transform to string) ──▶ channel2 ──▶ Stage3 (collect)
/// </code>
/// <para>
/// Each stage signals its downstream by calling <c>Writer.Complete()</c> when it
/// finishes processing. Downstream stages exit their <c>ReadAllAsync</c> loop
/// automatically when the upstream writer is completed and all buffered items
/// have been consumed.
/// </para>
/// </remarks>
public static class PipelinePattern
{
    /// <summary>
    /// Runs a three-stage pipeline:
    /// Stage 1 (Read): produces integers 1..<paramref name="itemCount"/>.
    /// Stage 2 (Transform): converts each int to a string "item-N".
    /// Stage 3 (Collect): accumulates results into a list.
    /// Each stage runs as an independent async Task.
    /// </summary>
    /// <param name="itemCount">Number of items to produce and process.</param>
    /// <param name="ct">Cancellation token for cooperative shutdown.</param>
    /// <returns>
    /// An ordered list of transformed strings, one per produced integer.
    /// </returns>
    public static async Task<List<string>> RunAsync(int itemCount, CancellationToken ct = default)
    {
        Channel<int> channel1 = Channel.CreateUnbounded<int>();
        Channel<string> channel2 = Channel.CreateUnbounded<string>();
        List<string> results = [];

        Task stage1 = Task.Run(async () =>
        {
            for (int i = 1; i <= itemCount; i++)
            {
                await channel1.Writer.WriteAsync(i, ct).ConfigureAwait(false);
            }

            channel1.Writer.Complete();
        }, ct);

        Task stage2 = Task.Run(async () =>
        {
            await foreach (int n in channel1.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                await channel2.Writer.WriteAsync($"item-{n}", ct).ConfigureAwait(false);
            }

            channel2.Writer.Complete();
        }, ct);

        Task stage3 = Task.Run(async () =>
        {
            await foreach (string item in channel2.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                results.Add(item);
            }
        }, ct);

        await Task.WhenAll(stage1, stage2, stage3).ConfigureAwait(false);

        return results;
    }
}
