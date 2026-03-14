// ============================================================
// Concept  : SynchronizationContext Deadlock
// Summary  : Calling .Result or .Wait() on a Task inside a thread that owns a SynchronizationContext causes a deadlock because the continuation cannot resume on the blocked context
// When to use   : As a learning example — understand why blocking on async code deadlocks in ASP.NET and WinForms
// When NOT to use: In production code — use await throughout the call chain and ConfigureAwait(false) in library code
// ============================================================

using System.Threading.Tasks;

namespace DotNet.Multithreading.Examples.Pitfalls;

/// <summary>
/// Illustrates the <c>.Result</c> / <c>.Wait()</c> deadlock that is endemic in legacy
/// ASP.NET 4.x and WinForms code when synchronous callers block on async methods.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this pattern is dangerous in ASP.NET 4.x and WinForms:</b>
/// Both frameworks install a <c>SynchronizationContext</c> that marshals continuations
/// back to a specific thread. When that thread is blocked by <c>.Result</c> or
/// <c>.Wait()</c>, the continuation queue never drains — the caught thread waits for
/// an operation whose completion event can never reach it.
/// </para>
/// <para>
/// <b>Why this does not deadlock in ASP.NET Core or xUnit:</b>
/// ASP.NET Core removed the per-request <c>SynchronizationContext</c>, and xUnit's
/// test runner similarly has no context. Continuations are therefore posted to the
/// <c>ThreadPool</c> and run freely, so <c>.Result</c> unblocks eventually. However
/// it still wastes a thread pool thread while waiting — the pattern remains a bad
/// practice even where it does not deadlock.
/// </para>
/// <para>
/// <b>Rule of thumb:</b> If a method is async, every caller must also be async.
/// Never mix <c>await</c> with <c>.Result</c> / <c>.Wait()</c> in the same call chain.
/// </para>
/// </remarks>
public static class ForgottenSynchronizationContextDemo
{
    /// <summary>
    /// Documents the anti-pattern of blocking on a <c>Task</c> with <c>.Result</c>
    /// or <c>.Wait()</c> from within a thread that owns a <c>SynchronizationContext</c>.
    /// </summary>
    /// <returns>
    /// A human-readable explanation of why this pattern deadlocks in ASP.NET 4.x,
    /// WinForms, and WPF — environments that use a single-threaded
    /// <c>SynchronizationContext</c>.
    /// </returns>
    /// <remarks>
    /// The method cannot reproduce the actual deadlock in a unit-test environment
    /// because xUnit runs without a <c>SynchronizationContext</c>. Refer to the
    /// inline comments for the problematic code pattern to avoid.
    /// </remarks>
    /// <example>
    /// <code>
    /// // ANTI-PATTERN (deadlocks in ASP.NET 4.x / WinForms):
    /// public string GetData()
    /// {
    ///     // .Result blocks the current thread (which owns the SynchronizationContext).
    ///     // The continuation of FetchDataAsync() needs that same thread → deadlock.
    ///     return FetchDataAsync().Result;
    /// }
    ///
    /// // FIX: propagate async all the way up
    /// public async Task&lt;string&gt; GetDataAsync()
    /// {
    ///     return await FetchDataAsync().ConfigureAwait(false);
    /// }
    /// </code>
    /// </example>
    public static string BlockingCallOnTaskInAsyncContext()
    {
        return
            "Calling .Result or .Wait() on a Task from within a SynchronizationContext " +
            "(ASP.NET classic, WinForms, WPF) causes a deadlock: the calling thread is " +
            "blocked waiting for the Task, but the Task's continuation needs that same " +
            "thread to resume — neither can proceed. Fix: use 'await' throughout the " +
            "call chain and add ConfigureAwait(false) in library code.";
    }

    /// <summary>
    /// Shows the correct asynchronous alternative to <c>BlockingCallOnTaskInAsyncContext</c>.
    /// The method is declared <c>async</c> and uses <c>await</c> so the calling thread
    /// is never blocked — it is released back to the pool while the operation is pending.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that resolves to a confirmation string once the
    /// asynchronous operation completes.
    /// </returns>
    /// <example>
    /// <code>
    /// string result = await ForgottenSynchronizationContextDemo.SafeAlternative();
    /// // result == "async result properly awaited — no SynchronizationContext deadlock"
    /// </code>
    /// </example>
    public static async Task<string> SafeAlternative()
    {
        // ConfigureAwait(false): the continuation does not need to return to the
        // caller's SynchronizationContext, which prevents the deadlock entirely
        // and avoids an unnecessary context-switch overhead in library code.
        await Task.Delay(0).ConfigureAwait(false);

        return "async result properly awaited — no SynchronizationContext deadlock";
    }
}
