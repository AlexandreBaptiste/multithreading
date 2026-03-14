// ============================================================
// Concept  : Volatile keyword and Volatile.Read / Volatile.Write
// Summary  : Lightweight per-variable memory fence that prevents compiler/JIT/CPU reordering and ensures fresh reads and immediate writes
// When to use   : When you need a simple stop-flag pattern between threads or acquire/release semantics on a single variable
// When NOT to use: When you need atomicity for compound operations (e.g., i++) or when mutation must be atomic — use Interlocked or lock instead
// ============================================================

using System.Threading;

namespace DotNet.Multithreading.Examples.AtomicOperations;

/// <summary>
/// Demonstrates the <c>volatile</c> keyword and explicit
/// <c>Volatile.Read</c> / <c>Volatile.Write</c> memory-fence primitives.
/// </summary>
/// <remarks>
/// <para>
/// <c>volatile</c> is a lightweight memory fence applied <em>per variable</em>. It prevents
/// the compiler, JIT, and CPU from caching the value in a CPU register across loop
/// iterations, ensuring every read fetches the latest value from main memory. This makes
/// it suitable for simple stop-flag patterns visible across threads.
/// </para>
/// <para>
/// <b>volatile does NOT make compound operations atomic.</b> An expression such as
/// <c>_counter++</c> compiles to three instructions (read, add, write) even when
/// <c>_counter</c> is <c>volatile</c>. Use <c>Interlocked</c> or <c>lock</c> for
/// atomic read-modify-write.
/// </para>
/// <para>
/// <b>Memory model:</b> <c>Volatile.Read</c> provides acquire semantics (no later access
/// can be reordered before it). <c>Volatile.Write</c> provides release semantics (no
/// earlier access can be reordered after it). On x86/x64 these are essentially free;
/// on ARM64 they emit real barrier instructions.
/// </para>
/// </remarks>
public static class VolatileDemo
{
    /// <summary>
    /// Private state container used by <c>TerminateWithVolatileFlag</c>.
    /// Encapsulating the <c>volatile</c> field in an instance class ensures that
    /// parallel test runs each operate on their own independent copy of the flag,
    /// avoiding cross-test state pollution.
    /// </summary>
    private sealed class StopState
    {
        /// <summary>
        /// Stop flag. Declared <c>volatile</c> so the background thread always sees
        /// the write performed by the main thread without JIT register-caching.
        /// </summary>
        public volatile bool Stop;
    }

    // Shared field for VolatileReadWrite — not used from multiple threads simultaneously.
    private static int _value;

    /// <summary>
    /// Starts a background thread that spins in a tight loop checking a
    /// <c>volatile bool</c> flag, then signals it to stop after 50 ms.
    /// </summary>
    /// <param name="timeoutMs">
    /// Maximum milliseconds to wait for the background thread to finish after the flag
    /// is set. Values around 2000 ms are typically safe on any CI machine.
    /// </param>
    /// <returns>
    /// <c>true</c> if the background loop terminated within <paramref name="timeoutMs"/>;
    /// <c>false</c> if it timed out (which would indicate a memory-visibility failure).
    /// </returns>
    /// <remarks>
    /// Without the <c>volatile</c> qualifier the JIT may hoist the <c>_stop</c> read out
    /// of the loop in Release builds, causing an infinite spin. The <c>volatile</c>
    /// keyword prevents this optimisation by forcing a fresh load on every iteration.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool terminated = VolatileDemo.TerminateWithVolatileFlag(2000);
    /// // terminated == true
    /// </code>
    /// </example>
    public static bool TerminateWithVolatileFlag(int timeoutMs)
    {
        StopState state = new();

        Thread worker = new(() =>
        {
            // The JIT cannot hoist _stop into a register because it is volatile.
            while (!state.Stop)
            {
                Thread.SpinWait(10);
            }
        });

        worker.IsBackground = true;
        worker.Start();

        // Let the worker get started, then signal it to stop.
        Thread.Sleep(50);
        state.Stop = true;

        return worker.Join(timeoutMs);
    }

    /// <summary>
    /// Demonstrates explicit <c>Volatile.Write</c> and <c>Volatile.Read</c> as an
    /// alternative to the <c>volatile</c> keyword.
    /// </summary>
    /// <returns>
    /// The value written (42), read back via <c>Volatile.Read</c>.
    /// </returns>
    /// <remarks>
    /// <c>Volatile.Write</c> / <c>Volatile.Read</c> are useful when the field cannot be
    /// marked <c>volatile</c> directly — for example, elements in an array or fields
    /// accessed via a generic type parameter.
    /// </remarks>
    /// <example>
    /// <code>
    /// int v = VolatileDemo.VolatileReadWrite();
    /// // v == 42
    /// </code>
    /// </example>
    public static int VolatileReadWrite()
    {
        // Release fence: ensures all prior writes are visible before this store.
        Volatile.Write(ref _value, 42);

        // Acquire fence: ensures this load is not reordered with subsequent reads.
        int readBack = Volatile.Read(ref _value);

        return readBack;
    }
}
