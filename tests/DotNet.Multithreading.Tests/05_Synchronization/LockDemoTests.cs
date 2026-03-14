using DotNet.Multithreading.Examples.Synchronization;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Synchronization;

public class LockDemoTests
{
    private readonly ITestOutputHelper _output;

    public LockDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void IncrementWithLock_10Threads100Increments_FinalCountIsCorrect()
    {
        // Arrange
        const int threadCount = 10;
        const int incrementsPerThread = 100;
        const int expected = threadCount * incrementsPerThread;

        // Act
        int result = LockDemo.IncrementWithLock(threadCount, incrementsPerThread);

        // Assert
        _output.WriteLine($"Final count: {result} (expected {expected})");
        result.Should().Be(expected);
    }

    [Fact]
    public void TryEnterWithTimeout_LockAlreadyHeld_ReturnsFalse()
    {
        // Arrange
        const int timeoutMs = 50; // holder keeps the lock for 200 ms

        // Act
        bool acquired = LockDemo.TryEnterWithTimeout(timeoutMs);

        // Assert
        _output.WriteLine($"Acquired within {timeoutMs} ms: {acquired}");
        acquired.Should().BeFalse();
    }

    [Fact]
    public void ProducerConsumerWithMonitor_20Items_ConsumesAll()
    {
        // Arrange
        const int itemCount = 20;

        // Act
        int consumed = LockDemo.ProducerConsumerWithMonitor(itemCount);

        // Assert
        _output.WriteLine($"Consumed: {consumed} / {itemCount}");
        consumed.Should().Be(itemCount);
    }
}
