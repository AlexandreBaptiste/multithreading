// ============================================================
// Concept  : AsyncLocal<T> vs ThreadLocal<T>
// Summary  : AsyncLocal<T> flows ambient context through await boundaries
//            because it is tied to the logical execution context (which
//            follows the async call chain). ThreadLocal<T> does NOT flow
//            through await — it is scoped to the physical OS thread, and
//            after an await the continuation may resume on a different thread.
// Rule of thumb : Use AsyncLocal<T> for ambient values that must survive
//                 await (e.g., correlation IDs, per-request user identity).
//                 Use ThreadLocal<T> only for thread-affine resources that
//                 must NOT be shared across threads.
// ============================================================

namespace DotNet.Multithreading.Examples.Patterns;

/// <summary>
/// Demonstrates the difference between <c>AsyncLocal&lt;T&gt;</c> and
/// <c>ThreadLocal&lt;T&gt;</c> with respect to async continuations.
/// </summary>
/// <remarks>
/// <para>
/// <b>AsyncLocal&lt;T&gt; — logical execution context:</b>
/// The value travels with the <em>logical</em> call chain. When you <c>await</c>
/// a task the runtime captures the current <see cref="System.Threading.ExecutionContext"/>
/// (which includes all <c>AsyncLocal</c> values) and restores it on the
/// continuation, regardless of which physical thread picks up the work.
/// </para>
/// <para>
/// <b>ThreadLocal&lt;T&gt; — physical thread:</b>
/// The value is stored in a slot on the currently executing OS thread. After
/// <c>await Task.Yield()</c> the continuation may run on a different thread
/// whose slot is empty (or carries a different value), so the value appears
/// as <c>null</c> from the caller's perspective.
/// </para>
/// <para>
/// <b>Child-task isolation:</b>
/// A child task (<c>Task.Run</c>) <em>inherits a snapshot</em> of the parent's
/// <c>AsyncLocal</c> values at the moment the child is created. Mutations made
/// inside the child do <em>not</em> propagate back to the parent — each has its
/// own independent copy of the execution context.
/// </para>
/// </remarks>
public static class AsyncLocalDemo
{
    /// <summary>
    /// Proves that an <c>AsyncLocal&lt;T&gt;</c> value set before an <c>await</c>
    /// is still visible after the continuation resumes, even if the thread changes.
    /// </summary>
    /// <returns>
    /// The value of the <c>AsyncLocal</c> after the awaited delay; always
    /// <c>"correlation-123"</c>.
    /// </returns>
    public static async Task<string> AsyncLocalFlowsAcrossAwait()
    {
        AsyncLocal<string?> asyncLocal = new();
        asyncLocal.Value = "correlation-123";

        await Task.Delay(1).ConfigureAwait(false);

        return asyncLocal.Value ?? "";
    }

    /// <summary>
    /// Proves that a child <c>Task.Run</c> inherits the parent's
    /// <c>AsyncLocal&lt;T&gt;</c> value at the moment the child is created.
    /// </summary>
    /// <returns>
    /// The value read <em>inside</em> the child task; always <c>"parent-value"</c>.
    /// </returns>
    public static async Task<string> AsyncLocalChildTaskGetsParentValue()
    {
        AsyncLocal<string?> asyncLocal = new();
        asyncLocal.Value = "parent-value";

        string childValue = await Task.Run(() => asyncLocal.Value ?? "").ConfigureAwait(false);

        return childValue;
    }

    /// <summary>
    /// Proves that a mutation made inside a child task does <em>not</em> propagate
    /// back to the parent's <c>AsyncLocal&lt;T&gt;</c> value, because each task
    /// operates on its own copy of the execution context.
    /// </summary>
    /// <returns>
    /// A tuple where <c>parent</c> is always <c>"original"</c> and
    /// <c>child</c> is always <c>"mutated"</c>.
    /// </returns>
    public static async Task<(string parent, string child)> AsyncLocalChildMutationDoesNotAffectParent()
    {
        AsyncLocal<string?> asyncLocal = new();
        asyncLocal.Value = "original";

        string childValue = await Task.Run(() =>
        {
            asyncLocal.Value = "mutated";
            return asyncLocal.Value ?? "";
        }).ConfigureAwait(false);

        string parentValue = asyncLocal.Value ?? "";

        return (parentValue, childValue);
    }

    /// <summary>
    /// Demonstrates that <c>ThreadLocal&lt;T&gt;</c> does <em>not</em> flow across
    /// an <c>await Task.Yield()</c>, because after the yield the continuation may
    /// resume on a different thread pool thread whose slot contains a different value.
    /// </summary>
    /// <returns>
    /// The value of the <c>ThreadLocal</c> after the yield; may be <c>null</c>
    /// or differ from the originally assigned value, proving that
    /// <c>ThreadLocal&lt;T&gt;</c> is thread-scoped, not context-scoped.
    /// </returns>
    public static async Task<string?> ThreadLocalDoesNotFlowAcrossAwait()
    {
        ThreadLocal<string?> threadLocal = new();
        threadLocal.Value = "thread-value";

        // Task.Yield forces the continuation onto a potentially different thread-pool thread.
        await Task.Yield();

        return threadLocal.Value;
    }
}
