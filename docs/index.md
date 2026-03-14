# .NET Multithreading Examples

Welcome to the **.NET Multithreading Examples** API reference.

This documentation is auto-generated from XML doc comments in the source code.
Every public class and method explains the concept, when to use it, its caveats, and how it works.

## Topics

| Module | Description |
|--------|-------------|
| `Basics` | Raw `Thread` API — lifecycle, parameters, priority |
| `ThreadPool` | `ThreadPool.QueueUserWorkItem`, thread reuse, overhead comparison |
| `Tasks` | TPL — `Task`, `Task.Factory`, combinators, PLINQ, cancellation |
| `AsyncAwait` | async/await patterns, `ValueTask`, `ConfigureAwait`, async streams |
| `Synchronization` | `lock`, `Mutex`, `SemaphoreSlim`, `ReaderWriterLockSlim`, `Barrier`, `SpinLock` |
| `AtomicOperations` | `Interlocked`, `volatile`, `Volatile.Read/Write`, memory barriers |
| `Pitfalls` | Race conditions, deadlocks, livelocks, thread starvation — broken & fixed |
| `ConcurrentCollections` | `ConcurrentDictionary`, `Channel<T>`, `BlockingCollection` |
| `Patterns` | Producer-consumer, pipeline, `AsyncLocal<T>`, `Lazy<T>`, throttled parallelism |

## Getting Started

```bash
dotnet test              # run all 96 example-proving tests
dotnet build             # build the solution
```
