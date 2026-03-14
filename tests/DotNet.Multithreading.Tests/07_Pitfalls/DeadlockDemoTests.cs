using DotNet.Multithreading.Examples.Pitfalls;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Pitfalls;

public class DeadlockDemoTests
{
    private readonly ITestOutputHelper _output;

    public DeadlockDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Broken_ABBALockOrdering_DeadlockDetected()
    {
        // Arrange
        const int timeoutMs = 500;

        // Act
        bool deadlockDetected = DeadlockDemo.Broken(timeoutMs);

        // Assert
        _output.WriteLine($"Deadlock detected: {deadlockDetected}");
        deadlockDetected.Should().BeTrue(
            because: "ABBA lock ordering creates a circular wait; at least one TryEnter must time out");
    }

    [Fact]
    public void Fixed_ConsistentLockOrdering_CompletesSuccessfully()
    {
        // Arrange
        const int iterations = 10;

        // Act
        int completed = DeadlockDemo.Fixed(iterations);

        // Assert
        _output.WriteLine($"Completed: {completed}");
        completed.Should().Be(iterations,
            because: "consistent lock ordering prevents circular wait; all iterations complete");
    }

    [Fact]
    public void TryEnterEscapeHatch_LockAlreadyHeld_ReturnsFalse()
    {
        // Arrange — no setup needed; the method creates its own lock and holder thread

        // Act
        bool acquired = DeadlockDemo.TryEnterEscapeHatch();

        // Assert
        _output.WriteLine($"Acquired already-held lock: {acquired}");
        acquired.Should().BeFalse(
            because: "the lock is held by the calling thread for the full 100 ms timeout; TryEnter must fail");
    }
}
