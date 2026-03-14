// ============================================================
// Concept  : ReaderWriterLockSlim
// Summary  : Demonstrates ReaderWriterLockSlim for read-heavy concurrent workloads where multiple readers proceed simultaneously but writers require exclusive access.
// When to use   : Read-heavy workloads with occasional writes. Multiple readers can hold the lock simultaneously.
// When NOT to use: Write-heavy workloads (plain lock performs better). Recursive usage needs care (use RecursionPolicy).
// ============================================================

namespace DotNet.Multithreading.Examples.Synchronization;

/// <summary>
/// Demonstrates <see cref="ReaderWriterLockSlim"/> for read-heavy concurrent workloads
/// where multiple readers may proceed simultaneously but writers require exclusive access.
/// </summary>
public static class ReaderWriterLockDemo
{
    /// <summary>
    /// Starts <paramref name="readerCount"/> threads, each acquiring the read lock concurrently,
    /// and returns the peak number of simultaneous readers observed.
    /// </summary>
    /// <param name="readerCount">Number of reader threads to spawn.</param>
    /// <returns>Peak concurrent reader count; equals <paramref name="readerCount"/> when all readers overlap.</returns>
    /// <example>
    /// <code>
    /// int peak = ReaderWriterLockDemo.ConcurrentReads(5);
    /// // peak == 5  — all five readers held the lock simultaneously
    /// </code>
    /// </example>
    public static int ConcurrentReads(int readerCount)
    {
        ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();
        // All readers signal here once inside the read lock so we can measure the peak.
        CountdownEvent allInsideLock = new CountdownEvent(readerCount);
        // Main thread sets this to release all readers.
        ManualResetEventSlim releaseReaders = new ManualResetEventSlim(false);

        int concurrent = 0;
        int peak = 0;

        Thread[] threads = new Thread[readerCount];
        for (int i = 0; i < readerCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                rwl.EnterReadLock();
                try
                {
                    Interlocked.Increment(ref concurrent);
                    allInsideLock.Signal();

                    // Stay inside the lock until released.
                    releaseReaders.Wait();

                    Interlocked.Decrement(ref concurrent);
                }
                finally
                {
                    rwl.ExitReadLock();
                }
            });
            threads[i].IsBackground = true;
            threads[i].Start();
        }

        // Wait until all readers are inside the lock, then capture the peak.
        allInsideLock.Wait(TimeSpan.FromSeconds(5));
        peak = Volatile.Read(ref concurrent);

        releaseReaders.Set();

        foreach (Thread t in threads)
        {
            t.Join();
        }

        rwl.Dispose();
        allInsideLock.Dispose();
        releaseReaders.Dispose();

        return peak;
    }

    /// <summary>
    /// Verifies that a writer cannot acquire the write lock while a reader holds the read lock.
    /// Starts a reader that holds the read lock for 100 ms, then immediately attempts
    /// <see cref="ReaderWriterLockSlim.TryEnterWriteLock(int)"/> with a 50 ms timeout from the
    /// calling thread.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> because the write lock acquisition should time out while
    /// the reader is active.
    /// </returns>
    /// <example>
    /// <code>
    /// bool acquired = ReaderWriterLockDemo.ExclusiveWrite_WriterIsBlocked_UntilReadersRelease();
    /// // acquired == false
    /// </code>
    /// </example>
    public static bool ExclusiveWrite_WriterIsBlocked_UntilReadersRelease()
    {
        ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();
        ManualResetEventSlim readerReady = new ManualResetEventSlim(false);

        Thread reader = new Thread(() =>
        {
            rwl.EnterReadLock();
            try
            {
                readerReady.Set();
                Thread.Sleep(100);
            }
            finally
            {
                rwl.ExitReadLock();
            }
        });
        reader.IsBackground = true;
        reader.Start();

        readerReady.Wait();

        bool writeLockAcquired = rwl.TryEnterWriteLock(50);

        if (writeLockAcquired)
        {
            rwl.ExitWriteLock();
        }

        reader.Join();
        rwl.Dispose();
        readerReady.Dispose();

        // Writer should NOT have acquired the lock.
        return writeLockAcquired;
    }

    /// <summary>
    /// Demonstrates the upgradeable read lock pattern: enters an upgradeable read lock,
    /// upgrades to a write lock, performs a write, then exits both locks in reverse order.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the entire upgrade/downgrade cycle completed without exception.
    /// </returns>
    /// <example>
    /// <code>
    /// bool ok = ReaderWriterLockDemo.UpgradeableLock();
    /// // ok == true
    /// </code>
    /// </example>
    public static bool UpgradeableLock()
    {
        ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();
        try
        {
            rwl.EnterUpgradeableReadLock();
            try
            {
                // Inspect shared state here (read phase).
                rwl.EnterWriteLock();
                try
                {
                    // Perform the write (mutation phase).
                }
                finally
                {
                    rwl.ExitWriteLock();
                }
            }
            finally
            {
                rwl.ExitUpgradeableReadLock();
            }

            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            rwl.Dispose();
        }
    }
}
