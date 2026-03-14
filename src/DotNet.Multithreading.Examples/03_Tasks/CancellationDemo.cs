// ============================================================
// Concept  : Cooperative Cancellation
// Summary  : Using CancellationToken and CancellationTokenSource to
//            cancel long-running operations safely and cooperatively.
// When to use   : Any operation that should be stoppable: async loops,
//                 PLINQ queries, Task.Run workloads, HTTP requests, etc.
// When NOT to use: Thread.Abort (deprecated/removed); hard-killing threads
//                  without cleanup. Always prefer cooperative cancellation.
// ============================================================

namespace DotNet.Multithreading.Examples.Tasks;

/// <summary>
/// Demonstrates the cooperative cancellation model offered by
/// <see cref="CancellationTokenSource"/> and <see cref="CancellationToken"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to use:</b> Pass a <see cref="CancellationToken"/> to every async
/// method and long-running loop so callers can request cancellation without
/// aborting threads.
/// </para>
/// <para>
/// <b>When NOT to use:</b> Do not use <c>Thread.Abort</c> (removed in .NET
/// Core); it bypasses finally blocks and leaves state inconsistent.
/// </para>
/// </remarks>
public static class CancellationDemo
{
    /// <summary>
    /// Creates a <see cref="CancellationTokenSource"/> that automatically
    /// cancels after <paramref name="cancelAfterMs"/> milliseconds, then
    /// runs an async loop that calls
    /// <see cref="CancellationToken.ThrowIfCancellationRequested"/> on each
    /// iteration.
    /// </summary>
    /// <param name="cancelAfterMs">
    /// Milliseconds after which the token is cancelled. Must be positive.
    /// </param>
    /// <returns>
    /// <c>"cancelled"</c> once the loop detects that the token has been
    /// cancelled.
    /// </returns>
    public static async Task<string> CancelAfterDelay(int cancelAfterMs)
    {
        using CancellationTokenSource cts = new();
        cts.CancelAfter(cancelAfterMs);
        CancellationToken token = cts.Token;

        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(1, token);
            }
        }
        catch (OperationCanceledException)
        {
            return "cancelled";
        }
    }

    /// <summary>
    /// Creates a <see cref="CancellationTokenSource"/> that automatically
    /// cancels after <paramref name="cancelAfterMs"/> milliseconds and polls
    /// <see cref="CancellationToken.IsCancellationRequested"/> without throwing.
    /// </summary>
    /// <param name="cancelAfterMs">
    /// Milliseconds after which the token is cancelled. Must be positive.
    /// </param>
    /// <returns>
    /// <c>"cancelled"</c> if the token is cancelled before the loop finishes;
    /// <c>"completed"</c> if the loop completes without cancellation.
    /// </returns>
    public static async Task<string> CheckIsCancellationRequested(int cancelAfterMs)
    {
        using CancellationTokenSource cts = new();
        cts.CancelAfter(cancelAfterMs);

        for (int i = 0; i < 1000; i++)
        {
            if (cts.Token.IsCancellationRequested)
            {
                return "cancelled";
            }

            await Task.Delay(1);
        }

        return "completed";
    }

    /// <summary>
    /// Creates two independent <see cref="CancellationTokenSource"/> instances,
    /// links them with
    /// <see cref="CancellationTokenSource.CreateLinkedTokenSource(CancellationToken[])"/>,
    /// cancels the first source, and confirms that the linked token is also
    /// cancelled.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the linked token reflects the cancellation of
    /// the first parent source; otherwise <see langword="false"/>.
    /// </returns>
    public static bool LinkedTokenSources()
    {
        using CancellationTokenSource cts1 = new();
        using CancellationTokenSource cts2 = new();
        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);

        cts1.Cancel();

        return linked.Token.IsCancellationRequested;
    }

    /// <summary>
    /// Demonstrates <see cref="CancellationToken.Register(Action)"/> by
    /// registering a callback that sets a flag when
    /// <paramref name="ct"/> is cancelled.
    /// </summary>
    /// <param name="ct">
    /// The cancellation token to observe. When already cancelled the callback
    /// fires synchronously before <c>Register</c> returns.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the registered callback was invoked;
    /// <see langword="false"/> if the token was not cancelled before the method
    /// returned.
    /// </returns>
    public static bool CooperativeCancellation(CancellationToken ct)
    {
        bool callbackFired = false;

        using CancellationTokenRegistration registration = ct.Register(
            () => callbackFired = true);

        return callbackFired;
    }
}
