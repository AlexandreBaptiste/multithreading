// ============================================================
// Concept  : Memory Barriers (Thread.MemoryBarrier, acquire/release semantics)
// Summary  : Prevents CPU and compiler from reordering memory operations across a fence point using acquire, release, or full-fence primitives
// When to use   : For advanced lock-free algorithms requiring fine-grained control over ordering semantics beyond what a single volatile access provides
// When NOT to use: For simple cases — prefer volatile or Interlocked instead
// ============================================================

using System.Threading;

namespace DotNet.Multithreading.Examples.AtomicOperations;

/// <summary>
/// Demonstrates full-fence and acquire/release memory-ordering primitives:
/// <c>Thread.MemoryBarrier()</c>, <c>Volatile.Read</c>, and <c>Volatile.Write</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Acquire semantics</b> (<c>Volatile.Read</c>): All memory operations that appear
/// after the read in program order are guaranteed to execute after the read. No later
/// access can be reordered before it.
/// </para>
/// <para>
/// <b>Release semantics</b> (<c>Volatile.Write</c>): All memory operations that appear
/// before the write in program order are guaranteed to complete before the write. No
/// earlier access can be reordered after it.
/// </para>
/// <para>
/// <b>Full fence</b> (<c>Thread.MemoryBarrier()</c>): Combines acquire and release —
/// no reordering in either direction across the barrier. On x86/x64 this prevents
/// compiler/JIT reordering; on ARM64 it also emits a hardware <c>DMB ISH</c>
/// instruction.
/// </para>
/// <para>
/// <b>x86/x64 vs ARM64:</b> x86/x64 uses Total Store Order (TSO) so barriers are
/// nearly free at runtime but still necessary to prevent JIT optimisations. ARM64
/// uses a weak memory model where barriers have measurable runtime cost.
/// </para>
/// </remarks>
public static class MemoryBarrierDemo
{
    private static int _field;

    // Fields used by FullFence to communicate between two threads.
    private static int _barrierX;
    private static int _barrierY;
    private static int _barrierReadX;
    private static int _barrierReadY;

    /// <summary>
    /// Uses <c>Thread.MemoryBarrier()</c> — a full memory fence — to prevent CPU and
    /// compiler reordering between two concurrently executing threads.
    /// </summary>
    /// <returns>
    /// Always <c>true</c>; the method exists to demonstrate that with barriers in place
    /// the classic "store–load reordering" scenario is prevented and the program
    /// produces a consistent result.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Without memory barriers on a weakly-ordered CPU (e.g., ARM64) it is possible
    /// for Thread 1 to see <c>y == 0</c> and Thread 2 to see <c>x == 0</c>
    /// simultaneously — an outcome that implies both stores were reordered after
    /// both loads. A full fence after each store prevents this.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// bool ok = MemoryBarrierDemo.FullFence();
    /// // ok == true
    /// </code>
    /// </example>
    public static bool FullFence()
    {
        _barrierX = 0;
        _barrierY = 0;
        _barrierReadX = 0;
        _barrierReadY = 0;

        using ManualResetEventSlim ready1 = new(false);
        using ManualResetEventSlim ready2 = new(false);
        using ManualResetEventSlim go = new(false);

        Thread thread1 = new(() =>
        {
            ready1.Set();
            go.Wait();

            _barrierX = 1;
            Thread.MemoryBarrier(); // Full fence — store cannot move after barrier.
            _barrierReadY = _barrierY;
        });

        Thread thread2 = new(() =>
        {
            ready2.Set();
            go.Wait();

            _barrierY = 1;
            Thread.MemoryBarrier(); // Full fence — store cannot move after barrier.
            _barrierReadX = _barrierX;
        });

        thread1.Start();
        thread2.Start();

        ready1.Wait();
        ready2.Wait();
        go.Set();

        thread1.Join();
        thread2.Join();

        // With full fences: at least one thread must have seen the other's write.
        // _readY==0 AND _readX==0 is impossible when barriers are present.
        bool consistent = !(_barrierReadY == 0 && _barrierReadX == 0);

        return consistent;
    }

    /// <summary>
    /// Writes <paramref name="value"/> to a shared field using <c>Volatile.Write</c>
    /// (release fence) and reads it back using <c>Volatile.Read</c> (acquire fence).
    /// </summary>
    /// <param name="value">The value to write and read back.</param>
    /// <returns>The value read back — always equal to <paramref name="value"/>.</returns>
    /// <remarks>
    /// This method is single-threaded on purpose: it isolates the acquire/release
    /// API surface. In real concurrent code, <c>Volatile.Write</c> on the producer
    /// and <c>Volatile.Read</c> on the consumer ensure the consumer sees all memory
    /// writes made before the producer's store.
    /// </remarks>
    /// <example>
    /// <code>
    /// int v = MemoryBarrierDemo.VolatileExplicitFence(7);
    /// // v == 7
    /// </code>
    /// </example>
    public static int VolatileExplicitFence(int value)
    {
        // Release fence: all preceding writes are visible to other threads after this.
        Volatile.Write(ref _field, value);

        // Acquire fence: this load cannot be reordered with subsequent reads.
        int readBack = Volatile.Read(ref _field);

        return readBack;
    }
}
