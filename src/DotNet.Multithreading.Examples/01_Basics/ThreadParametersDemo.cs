// ============================================================
// Concept  : Passing Parameters to Threads
// Summary  : Three patterns for supplying start-up data to a raw Thread
// When to use   : When using raw Thread and needing to pass data at creation time
// When NOT to use: For Task-based work use closures or method parameters
//                  directly — they are cleaner and fully type-safe.
// ============================================================

namespace DotNet.Multithreading.Examples.Basics;

/// <summary>
/// Demonstrates three patterns for passing data to a <see cref="Thread"/>
/// at start time, plus the infamous loop-closure capture gotcha and its fix.
/// </summary>
/// <remarks>
/// <para>
/// <b>Patterns covered:</b>
/// <list type="bullet">
///   <item><description>
///     <see cref="ParameterizedThreadStart"/> — passes an <see langword="object"/>
///     that must be cast inside the thread delegate.
///   </description></item>
///   <item><description>
///     Lambda closure — the lambda captures local state from the enclosing scope
///     directly, with no cast required.
///   </description></item>
///   <item><description>
///     State object — a dedicated, strongly-typed class carries all input and
///     output across the thread boundary.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <b>Closure gotcha:</b> capturing a loop variable by reference causes all
/// threads to observe the variable's final value. Always copy the loop variable
/// into a local before creating the lambda.
/// </para>
/// </remarks>
public static class ThreadParametersDemo
{
    /// <summary>
    /// Demonstrates <see cref="ParameterizedThreadStart"/>: the value is passed
    /// as <see langword="object"/> via <see cref="Thread.Start(object?)"/> and
    /// must be explicitly cast inside the thread delegate.
    /// </summary>
    /// <param name="input">An integer value to double inside the worker thread.</param>
    /// <returns>The input value doubled.</returns>
    public static int UsingParameterizedThreadStart(int input)
    {
        int result = 0;
        using ManualResetEventSlim done = new(false);

        Thread worker = new(new ParameterizedThreadStart(state =>
        {
            int value = (int)state!;
            result = value * 2;
            done.Set();
        }));

        worker.Start(input);
        done.Wait();

        return result;
    }

    /// <summary>
    /// Demonstrates the lambda-closure pattern: the lambda captures
    /// <paramref name="input"/> directly from the enclosing scope.
    /// This is the most ergonomic pattern — no boxing or casting required.
    /// </summary>
    /// <param name="input">An integer value to double inside the worker thread.</param>
    /// <returns>The input value doubled.</returns>
    public static int UsingLambdaClosure(int input)
    {
        int result = 0;
        using ManualResetEventSlim done = new(false);

        Thread worker = new(() =>
        {
            result = input * 2;
            done.Set();
        });

        worker.Start();
        done.Wait();

        return result;
    }

    /// <summary>
    /// Demonstrates the state-object pattern: a strongly-typed
    /// <see cref="WorkerState"/> instance is passed through
    /// <see cref="Thread.Start(object?)"/> and carries the result back.
    /// </summary>
    /// <param name="input">An integer value to double inside the worker thread.</param>
    /// <returns>The input value doubled.</returns>
    public static int UsingStateObject(int input)
    {
        WorkerState state = new() { Input = input };
        using ManualResetEventSlim done = new(false);

        Thread worker = new(new ParameterizedThreadStart(obj =>
        {
            WorkerState s = (WorkerState)obj!;
            s.Result = s.Input * 2;
            done.Set();
        }));

        worker.Start(state);
        done.Wait();

        return state.Result;
    }

    /// <summary>
    /// Illustrates the loop-variable closure bug: the lambda captures <c>i</c>
    /// by reference rather than by value. All threads block on a gate until the
    /// loop completes, at which point <c>i</c> equals <c>5</c> — so every
    /// element in the returned array is <c>5</c> instead of its index.
    /// </summary>
    /// <returns>
    /// An array of 5 integers that are all equal to <c>5</c> (the post-loop value
    /// of the loop variable), demonstrating the capture bug.
    /// </returns>
    /// <remarks>
    /// The gate ensures threads read <c>i</c> only after the loop has finished,
    /// making the bug deterministic and reliably observable in tests.
    /// </remarks>
    public static IReadOnlyList<int> ClosureCaptureGotcha()
    {
        const int count = 5;
        int[] captured = new int[count];
        using ManualResetEventSlim gate = new(false);
        using CountdownEvent done = new(count);

        for (int i = 0; i < count; i++)
        {
            int slot = i; // local copy used only as the write index
            Thread t = new(() =>
            {
                gate.Wait();
                captured[slot] = i; // BUG: i is the loop variable, captured by reference
                done.Signal();
            })
            {
                IsBackground = true
            };
            t.Start();
        }

        // Release all threads after the loop — i is now count (5)
        gate.Set();
        done.Wait();

        return Array.AsReadOnly(captured);
    }

    /// <summary>
    /// Fixes the loop-variable closure bug by copying <c>i</c> into a local
    /// variable <c>localI</c> before the lambda is constructed. Each iteration
    /// creates an independent copy, so each thread captures its own value.
    /// </summary>
    /// <returns>
    /// An array <c>[0, 1, 2, 3, 4]</c> — each element equals its index.
    /// </returns>
    public static IReadOnlyList<int> ClosureCaptureFix()
    {
        const int count = 5;
        int[] captured = new int[count];
        using ManualResetEventSlim gate = new(false);
        using CountdownEvent done = new(count);

        for (int i = 0; i < count; i++)
        {
            int localI = i; // FIX: each iteration captures its own independent copy
            Thread t = new(() =>
            {
                gate.Wait();
                captured[localI] = localI; // both index and value from the local copy
                done.Signal();
            })
            {
                IsBackground = true
            };
            t.Start();
        }

        gate.Set();
        done.Wait();

        return Array.AsReadOnly(captured);
    }

    /// <summary>
    /// Internal state object used by <see cref="UsingStateObject"/>.
    /// Carries both the input parameter and the output result across the
    /// thread boundary without boxing or casting of primitives.
    /// </summary>
    private sealed class WorkerState
    {
        /// <summary>Gets or sets the input value for the worker thread.</summary>
        public int Input { get; set; }

        /// <summary>Gets or sets the result produced by the worker thread.</summary>
        public int Result { get; set; }
    }
}
