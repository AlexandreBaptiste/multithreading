// ============================================================
// Concept  : Thread Priority
// Summary  : Demonstrates ThreadPriority — a scheduling hint to the OS
// When to use   : Very rarely; only to influence relative CPU time for
//                 long-running background threads.
// When NOT to use: For correctness, synchronisation, or time guarantees.
//                  Misuse can cause priority inversion or thread starvation.
// ============================================================

namespace DotNet.Multithreading.Examples.Basics;

/// <summary>
/// Demonstrates the <see cref="ThreadPriority"/> enum and
/// the <see cref="Thread.Priority"/> property.
/// </summary>
/// <remarks>
/// <para>
/// <b>Important:</b> Thread priority is a <em>hint</em> to the operating-system
/// scheduler — it does not guarantee execution order. Never write tests or
/// production logic that depends on threads running in priority order.
/// </para>
/// <para>
/// <b>When to use:</b> Very rarely — only when you need to influence relative
/// CPU time between long-running background threads (e.g., rendering vs.
/// housekeeping).
/// </para>
/// <para>
/// <b>When NOT to use:</b> For correctness, synchronisation, or hard timing
/// guarantees. Misuse can cause priority inversion or starvation of lower-priority
/// threads on heavily loaded systems.
/// </para>
/// </remarks>
public static class ThreadPriorityDemo
{
    /// <summary>
    /// Returns all defined <see cref="ThreadPriority"/> enum values in ascending
    /// declaration order: <c>Lowest</c>, <c>BelowNormal</c>, <c>Normal</c>,
    /// <c>AboveNormal</c>, <c>Highest</c>.
    /// </summary>
    /// <returns>All five <see cref="ThreadPriority"/> values.</returns>
    /// <example>
    /// <code>
    /// IReadOnlyList&lt;ThreadPriority&gt; all = ThreadPriorityDemo.GetAllPriorityValues();
    /// // [Lowest, BelowNormal, Normal, AboveNormal, Highest]
    /// </code>
    /// </example>
    public static IReadOnlyList<ThreadPriority> GetAllPriorityValues()
        => Enum.GetValues<ThreadPriority>();

    /// <summary>
    /// Creates a background thread, assigns the given <paramref name="priority"/>,
    /// starts it, and returns the <see cref="Thread.Priority"/> value observed
    /// from <see cref="Thread.CurrentThread"/> inside the worker thread.
    /// </summary>
    /// <param name="priority">The priority to assign to the new thread.</param>
    /// <returns>The priority as read from inside the worker thread.</returns>
    /// <remarks>
    /// Setting priority is applied immediately when <see cref="Thread.Priority"/>
    /// is assigned; the OS scheduler picks it up on the next scheduling decision.
    /// </remarks>
    /// <example>
    /// <code>
    /// ThreadPriority observed = ThreadPriorityDemo.SetAndReadPriority(ThreadPriority.AboveNormal);
    /// Console.WriteLine(observed); // AboveNormal
    /// </code>
    /// </example>
    public static ThreadPriority SetAndReadPriority(ThreadPriority priority)
    {
        ThreadPriority observed = default;
        using ManualResetEventSlim done = new(false);

        Thread worker = new(() =>
        {
            observed = Thread.CurrentThread.Priority;
            done.Set();
        })
        {
            Priority = priority,
            IsBackground = true
        };

        worker.Start();
        done.Wait();

        return observed;
    }
}
