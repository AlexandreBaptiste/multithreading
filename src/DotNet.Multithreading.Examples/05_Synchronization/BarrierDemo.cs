// ============================================================
// Concept  : Barrier
// Summary  : Demonstrates Barrier for synchronising multiple threads across discrete phases of a parallel algorithm.
// When to use   : Phased parallel algorithms where all participants must complete each phase before any proceeds.
// When NOT to use: Simple producer-consumer or one-time signalling — use CountdownEvent or ManualResetEvent instead.
// ============================================================

namespace DotNet.Multithreading.Examples.Synchronization;

/// <summary>
/// Demonstrates <see cref="Barrier"/> for synchronising multiple threads across discrete phases
/// of a parallel algorithm. All participants must call <see cref="Barrier.SignalAndWait()"/>
/// before any of them can advance to the next phase.
/// </summary>
public static class BarrierDemo
{
    /// <summary>
    /// Runs a phased parallel algorithm using a <see cref="Barrier"/> with a post-phase action
    /// that increments a counter on each phase completion.
    /// </summary>
    /// <param name="participantCount">Number of parallel participants (threads).</param>
    /// <param name="phaseCount">Number of phases each participant must complete.</param>
    /// <returns>
    /// Total number of phases completed; equals <paramref name="phaseCount"/> because the
    /// post-phase action fires exactly once per completed phase regardless of participant count.
    /// </returns>
    /// <example>
    /// <code>
    /// int phases = BarrierDemo.PhaseBarrier(4, 3);
    /// // phases == 3
    /// </code>
    /// </example>
    public static int PhaseBarrier(int participantCount, int phaseCount)
    {
        int phaseCounter = 0;

        using Barrier barrier = new Barrier(participantCount, _ =>
        {
            Interlocked.Increment(ref phaseCounter);
        });

        Thread[] threads = new Thread[participantCount];
        for (int i = 0; i < participantCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int phase = 0; phase < phaseCount; phase++)
                {
                    // Simulate per-phase work.
                    Thread.SpinWait(500);
                    barrier.SignalAndWait();
                }
            });
            threads[i].IsBackground = true;
            threads[i].Start();
        }

        foreach (Thread t in threads)
        {
            t.Join();
        }

        return phaseCounter;
    }

    /// <summary>
    /// Creates a <see cref="Barrier"/> with the specified number of participants and returns
    /// its <see cref="Barrier.ParticipantCount"/> property.
    /// </summary>
    /// <param name="participantCount">The number of participants to initialise the barrier with.</param>
    /// <returns>The value of <see cref="Barrier.ParticipantCount"/>; equals <paramref name="participantCount"/>.</returns>
    /// <example>
    /// <code>
    /// int count = BarrierDemo.BarrierParticipantCount(4);
    /// // count == 4
    /// </code>
    /// </example>
    public static int BarrierParticipantCount(int participantCount)
    {
        using Barrier barrier = new Barrier(participantCount);
        return barrier.ParticipantCount;
    }
}
