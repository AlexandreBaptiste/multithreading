// ============================================================
// Concept  : lock keyword / Monitor
// Summary  : Demonstrates the lock keyword (syntactic sugar for Monitor.Enter/Exit in try/finally), Monitor.TryEnter with timeout, and Monitor.Wait/Pulse for producer-consumer signalling.
// When to use   : Protecting shared data accessed by multiple threads when the critical section is short.
// When NOT to use: Do NOT lock on 'this', typeof(T), or public objects. Prefer SemaphoreSlim for async code.
// ============================================================

namespace DotNet.Multithreading.Examples.Synchronization;

/// <summary>
/// Demonstrates mutual exclusion using the <c>lock</c> keyword and <see cref="System.Threading.Monitor"/>.
/// </summary>
/// <remarks>
/// The <c>lock</c> statement is syntactic sugar for <see cref="System.Threading.Monitor.Enter(object)"/>
/// and <see cref="System.Threading.Monitor.Exit"/> wrapped in a <c>try/finally</c> block,
/// guaranteeing the lock is always released even if an exception is thrown.
/// </remarks>
public static class LockDemo
{
    /// <summary>
    /// Spawns <paramref name="threadCount"/> threads, each incrementing a shared counter
    /// <paramref name="incrementsPerThread"/> times using <c>lock</c>, and returns the final value.
    /// </summary>
    /// <param name="threadCount">Number of threads to spawn.</param>
    /// <param name="incrementsPerThread">Number of increments each thread performs.</param>
    /// <returns>
    /// The final counter value, which is guaranteed to equal
    /// <c>threadCount * incrementsPerThread</c> because every increment is serialised.
    /// </returns>
    /// <example>
    /// <code>
    /// int result = LockDemo.IncrementWithLock(10, 100);
    /// // result == 1000
    /// </code>
    /// </example>
    public static int IncrementWithLock(int threadCount, int incrementsPerThread)
    {
        int counter = 0;
        object syncRoot = new();

        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < incrementsPerThread; j++)
                {
                    lock (syncRoot)
                    {
                        counter++;
                    }
                }
            });
        }

        foreach (Thread t in threads)
        {
            t.Start();
        }

        foreach (Thread t in threads)
        {
            t.Join();
        }

        return counter;
    }

    /// <summary>
    /// Same as <see cref="IncrementWithLock"/> but WITHOUT any synchronisation — the result
    /// may be less than <c>threadCount * incrementsPerThread</c> due to race conditions.
    /// </summary>
    /// <param name="threadCount">Number of threads to spawn.</param>
    /// <param name="incrementsPerThread">Number of increments each thread performs.</param>
    /// <returns>
    /// The (potentially incorrect) final counter value demonstrating a data race.
    /// </returns>
    /// <remarks>
    /// This method intentionally omits locking to illustrate the consequences of unsynchronised
    /// access to shared mutable state. Do not rely on the returned value in production code.
    /// </remarks>
    public static int IncrementWithoutLock(int threadCount, int incrementsPerThread)
    {
        int counter = 0;

        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < incrementsPerThread; j++)
                {
                    counter++;
                }
            });
        }

        foreach (Thread t in threads)
        {
            t.Start();
        }

        foreach (Thread t in threads)
        {
            t.Join();
        }

        return counter;
    }

    /// <summary>
    /// Demonstrates <see cref="System.Threading.Monitor.TryEnter(object, int)"/> with a timeout.
    /// A background thread acquires the lock and holds it for 200 ms while the calling thread
    /// tries to acquire it with the given <paramref name="timeoutMs"/>.
    /// </summary>
    /// <param name="timeoutMs">
    /// Milliseconds the calling thread will wait before giving up.
    /// Use a value less than 200 (e.g., 50) to reliably observe a timeout.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the lock was acquired; <see langword="false"/> if the timeout
    /// elapsed before the lock became available.
    /// </returns>
    /// <example>
    /// <code>
    /// bool acquired = LockDemo.TryEnterWithTimeout(50);
    /// // acquired == false — lock was held by the background thread
    /// </code>
    /// </example>
    public static bool TryEnterWithTimeout(int timeoutMs)
    {
        object lockObj = new();
        ManualResetEventSlim lockHeld = new(false);

        Thread holder = new(() =>
        {
            lock (lockObj)
            {
                lockHeld.Set();
                Thread.Sleep(200);
            }
        });

        holder.Start();
        lockHeld.Wait(); // ensure the holder has the lock before we try

        bool acquired = Monitor.TryEnter(lockObj, timeoutMs);

        if (acquired)
        {
            Monitor.Exit(lockObj);
        }

        holder.Join();

        return acquired;
    }

    /// <summary>
    /// Shows a classic producer–consumer pattern implemented with <c>lock</c>,
    /// <see cref="System.Threading.Monitor.Wait(object)"/>, and
    /// <see cref="System.Threading.Monitor.Pulse(object)"/>.
    /// </summary>
    /// <param name="itemCount">Number of items the producer enqueues.</param>
    /// <returns>The total number of items the consumer successfully dequeued.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="System.Threading.Monitor.Wait(object)"/> atomically releases the lock and suspends the
    /// calling thread until it is pulsed, then re-acquires the lock before returning.
    /// </para>
    /// <para>
    /// <see cref="System.Threading.Monitor.Pulse"/> wakes exactly one waiting thread.
    /// Use <see cref="System.Threading.Monitor.PulseAll"/> when multiple threads may be waiting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// int consumed = LockDemo.ProducerConsumerWithMonitor(20);
    /// // consumed == 20
    /// </code>
    /// </example>
    public static int ProducerConsumerWithMonitor(int itemCount)
    {
        Queue<int> queue = new();
        object syncRoot = new();
        bool producerDone = false;
        int consumedCount = 0;

        Thread consumer = new(() =>
        {
            lock (syncRoot)
            {
                while (!producerDone || queue.Count > 0)
                {
                    while (queue.Count == 0 && !producerDone)
                    {
                        Monitor.Wait(syncRoot);
                    }

                    while (queue.Count > 0)
                    {
                        queue.Dequeue();
                        consumedCount++;
                    }
                }
            }
        });

        consumer.Start();

        Thread producer = new(() =>
        {
            for (int i = 0; i < itemCount; i++)
            {
                lock (syncRoot)
                {
                    queue.Enqueue(i);
                    Monitor.Pulse(syncRoot);
                }

                Thread.Sleep(1); // yield to let consumer process
            }

            lock (syncRoot)
            {
                producerDone = true;
                Monitor.Pulse(syncRoot);
            }
        });

        producer.Start();
        producer.Join();
        consumer.Join();

        return consumedCount;
    }
}
