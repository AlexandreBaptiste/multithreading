// ============================================================
// Concept  : Thread-safe lazy initialization
// Summary  : Prefer Lazy<T> over manual double-checked locking.
// Why DCL is dangerous: Without the volatile keyword on the backing field
//   the pre-.NET 2.0 memory model allowed a partially constructed object to
//   be observed by a competing thread. Even in .NET 2.0+ with the stronger
//   memory model, hand-rolled DCL is error-prone and hard to review.
//   Lazy<T> encapsulates the correct pattern and makes intent explicit.
// ============================================================

namespace DotNet.Multithreading.Examples.Patterns;

/// <summary>
/// Demonstrates thread-safe lazy initialization using <c>Lazy&lt;T&gt;</c>
/// and <c>LazyInitializer.EnsureInitialized</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Double-Checked Locking (DCL) anti-pattern:</b>
/// Before .NET 2.0 the memory model did not guarantee that writes to a field
/// were visible to all threads in the correct order. A thread could observe a
/// non-null reference to an object whose constructor had not yet finished
/// executing. Adding <c>volatile</c> fixes the visibility problem at the cost of
/// a memory barrier on every read. <c>Lazy&lt;T&gt;</c> implements DCL correctly
/// under the hood and removes the risk of subtle bugs.
/// </para>
/// <para>
/// <b>LazyThreadSafetyMode comparison:</b>
/// </para>
/// <list type="table">
///   <item>
///     <term><c>None</c></term>
///     <description>No thread safety; fastest. Use only when the lazy is
///     accessed from a single thread.</description>
///   </item>
///   <item>
///     <term><c>PublicationOnly</c></term>
///     <description>The factory may run multiple times on competing threads; the
///     first value to be published wins and all others are discarded. Suitable
///     for referentially-transparent (idempotent) factories.</description>
///   </item>
///   <item>
///     <term><c>ExecutionAndPublication</c></term>
///     <description>The factory runs exactly once; all other threads block until
///     the first initialization completes. This is the default and safest
///     option.</description>
///   </item>
/// </list>
/// </remarks>
public static class LazyInitializationDemo
{
    /// <summary>
    /// Returns an expensively-computed value that is initialized exactly once
    /// using <c>Lazy&lt;T&gt;</c> with <c>LazyThreadSafetyMode.ExecutionAndPublication</c>.
    /// </summary>
    /// <returns>The lazily initialized value (always 42).</returns>
    /// <example>
    /// <code>
    /// int value = LazyInitializationDemo.GetLazyValue();
    /// // value == 42
    /// </code>
    /// </example>
    public static int GetLazyValue()
    {
        Lazy<int> lazy = new(ExpensiveComputation, LazyThreadSafetyMode.ExecutionAndPublication);

        return lazy.Value;
    }

    /// <summary>
    /// Demonstrates that concurrent reads all receive the same lazily-initialized
    /// value regardless of which thread wins the initialization race.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads to spawn.</param>
    /// <returns>
    /// An array of results collected from all threads; every element must
    /// equal 42 because <c>Lazy&lt;T&gt;</c> ensures single initialization.
    /// </returns>
    public static int[] GetLazyValueConcurrently(int threadCount)
    {
        Lazy<int> lazy = new(ExpensiveComputation, LazyThreadSafetyMode.ExecutionAndPublication);
        int[] results = new int[threadCount];

        Thread[] threads = Enumerable.Range(0, threadCount)
            .Select(i => new Thread(() => results[i] = lazy.Value))
            .ToArray();

        foreach (Thread t in threads)
        {
            t.Start();
        }

        foreach (Thread t in threads)
        {
            t.Join();
        }

        return results;
    }

    /// <summary>
    /// Demonstrates <c>LazyInitializer.EnsureInitialized</c> as a lightweight
    /// alternative to <c>Lazy&lt;T&gt;</c> when you already hold a nullable field.
    /// The method initializes a reference-type resource exactly once and returns
    /// its value (99).
    /// </summary>
    /// <returns>
    /// The initialized value (always 99).
    /// </returns>
    public static int EnsureInitializedExample()
    {
        // EnsureInitialized<T> requires a reference type; we use a small
        // private wrapper to demonstrate the pattern with a value payload.
        ExpensiveResource? resource = null;
        ExpensiveResource initialized = LazyInitializer.EnsureInitialized(
            ref resource,
            () => new ExpensiveResource(99));

        return initialized.Value;
    }

    /// <summary>
    /// Returns a human-readable description of all three
    /// <c>LazyThreadSafetyMode</c> values and their appropriate use cases.
    /// </summary>
    /// <returns>A multi-line description string.</returns>
    public static string LazyModeComparison()
    {
        return
            "None: no thread safety, fastest, use only when lazy is accessed from one thread.\n" +
            "PublicationOnly: factory may run multiple times, first published value wins " +
            "(referentially transparent factories only).\n" +
            "ExecutionAndPublication: factory runs exactly once (default, safest).";
    }

    private static int ExpensiveComputation()
    {
        // Simulates an expensive calculation that should only run once.
        return 42;
    }

    /// <summary>
    /// A simple reference-type wrapper used to demonstrate
    /// <c>LazyInitializer.EnsureInitialized&lt;T&gt;</c>, which requires T
    /// to be a reference type.
    /// </summary>
    private sealed class ExpensiveResource
    {
        /// <summary>Gets the payload value.</summary>
        internal int Value { get; }

        internal ExpensiveResource(int value) => Value = value;
    }
}
