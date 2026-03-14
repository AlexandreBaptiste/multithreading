// ============================================================
// Concept  : Async Streams (IAsyncEnumerable<T>)
// Summary  : Producer/consumer pattern over asynchronous sequences
//            using C# async iterators (yield return) and await foreach.
// When to use   : Streaming large or infinite data sources (database rows,
//                 paged APIs, sensor feeds) where materialising the whole
//                 sequence at once is undesirable.
// When NOT to use: When the full result set is small and fits comfortably
//                  in memory — a plain Task<List<T>> is simpler and has
//                  lower overhead. Also avoid when the consumer must
//                  randomly access items.
// ============================================================

using System.Runtime.CompilerServices;

namespace DotNet.Multithreading.Examples.AsyncAwait;

/// <summary>
/// Demonstrates asynchronous streams using
/// <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/>, C# async
/// iterators with <c>yield return</c>, and <c>await foreach</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>How it works:</b> A method that returns
/// <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> and uses
/// <c>yield return</c> is compiled into an async iterator state machine.
/// Each <c>yield return</c> suspends the producer until the consumer calls
/// <c>MoveNextAsync</c> on the enumerator; the consumer drives the pace,
/// applying natural back-pressure.
/// </para>
/// <para>
/// <b>Cancellation:</b> Decorate the <c>CancellationToken</c> parameter
/// with <c>[EnumeratorCancellation]</c>. This attribute wires the token
/// passed to <c>WithCancellation(token)</c> directly into the iterator
/// state machine, propagating cancellation seamlessly.
/// </para>
/// </remarks>
public static class AsyncStreams
{
    /// <summary>
    /// Produces an asynchronous sequence of integers from <c>0</c> to
    /// <c><paramref name="count"/> - 1</c>, awaiting a brief delay between
    /// each item to simulate asynchronous work (e.g., a database or network
    /// fetch).
    /// </summary>
    /// <param name="count">The number of integers to yield.</param>
    /// <param name="ct">
    /// A <see cref="System.Threading.CancellationToken"/> that can cancel the
    /// iteration. Decorated with
    /// <see cref="System.Runtime.CompilerServices.EnumeratorCancellationAttribute"/>
    /// so that <c>WithCancellation(token)</c> propagates the token into the
    /// iterator.
    /// </param>
    /// <returns>
    /// An <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> that
    /// yields integers <c>0</c> through <c>count - 1</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// await foreach (int n in AsyncStreams.GenerateNumbersAsync(5))
    /// {
    ///     Console.WriteLine(n); // 0, 1, 2, 3, 4
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<int> GenerateNumbersAsync(
        int count,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 0; i < count; i++)
        {
            // Simulate async I/O between producing each item.
            await Task.Delay(1, ct);

            yield return i;
        }
    }

    /// <summary>
    /// Consumes the async stream produced by
    /// <see cref="GenerateNumbersAsync"/> and collects all yielded values
    /// into a list.
    /// </summary>
    /// <param name="count">The number of integers to generate and collect.</param>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task{TResult}"/> producing a
    /// <see cref="System.Collections.Generic.List{T}"/> containing all
    /// integers yielded by the stream.
    /// </returns>
    /// <example>
    /// <code>
    /// List&lt;int&gt; values = await AsyncStreams.ConsumeStreamAsync(5);
    /// // values == [0, 1, 2, 3, 4]
    /// </code>
    /// </example>
    public static async Task<List<int>> ConsumeStreamAsync(int count)
    {
        List<int> results = [];

        await foreach (int n in GenerateNumbersAsync(count))
        {
            results.Add(n);
        }

        return results;
    }

    /// <summary>
    /// Consumes the async stream but cancels enumeration after
    /// <paramref name="cancelAfter"/> items have been received, demonstrating
    /// the <c>WithCancellation</c> extension method.
    /// </summary>
    /// <param name="count">The total number of integers the stream would produce.</param>
    /// <param name="cancelAfter">
    /// The number of items to receive before requesting cancellation.
    /// </param>
    /// <returns>
    /// A <see cref="System.Threading.Tasks.Task{TResult}"/> producing the
    /// number of items actually received before cancellation took effect.
    /// The value is &lt;= <paramref name="cancelAfter"/>.
    /// </returns>
    /// <example>
    /// <code>
    /// int received = await AsyncStreams.ConsumeWithCancellationAsync(100, 3);
    /// // received &lt;= 3
    /// </code>
    /// </example>
    public static async Task<int> ConsumeWithCancellationAsync(int count, int cancelAfter)
    {
        using CancellationTokenSource cts = new();
        int received = 0;

        try
        {
            await foreach (int n in GenerateNumbersAsync(count).WithCancellation(cts.Token))
            {
                received++;

                if (received >= cancelAfter)
                {
                    // Signal cancellation — the iterator will throw
                    // OperationCanceledException on its next delay.
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected: cancellation propagated from inside the iterator.
        }

        return received;
    }
}
