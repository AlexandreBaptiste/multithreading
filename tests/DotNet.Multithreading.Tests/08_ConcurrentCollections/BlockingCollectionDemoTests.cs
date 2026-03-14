using DotNet.Multithreading.Examples.ConcurrentCollections;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.ConcurrentCollections;

public class BlockingCollectionDemoTests
{
    private readonly ITestOutputHelper _output;

    public BlockingCollectionDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ProducerConsumer_100Items_AllConsumed()
    {
        // Arrange
        const int itemCount = 100;
        const int boundedCapacity = 10;
        int expectedSum = itemCount * (itemCount - 1) / 2; // 0+1+…+99

        // Act
        int sum = BlockingCollectionDemo.ProducerConsumer(itemCount, boundedCapacity);

        // Assert
        _output.WriteLine($"Consumed sum: {sum} (expected: {expectedSum})");
        sum.Should().Be(expectedSum, "all items should be produced and consumed exactly once");
    }

    [Fact]
    public void TryTakeWithTimeout_EmptyCollection_ReturnsFalse()
    {
        // Arrange
        const int timeoutMs = 50;

        // Act
        bool taken = BlockingCollectionDemo.TryTakeWithTimeout(timeoutMs);

        // Assert
        _output.WriteLine($"TryTake result (empty collection, {timeoutMs} ms timeout): {taken}");
        taken.Should().BeFalse("no item is available and the timeout expires");
    }

    [Fact]
    public void BoundedCollectionBlocksProducer_FullCollection_AddTimesOut()
    {
        // Arrange
        const int capacity = 5;

        // Act
        bool added = BlockingCollectionDemo.BoundedCollectionBlocksProducer(capacity);

        // Assert
        _output.WriteLine($"TryAdd result (full collection, capacity={capacity}): {added}");
        added.Should().BeFalse("the collection is full and the timed add should time out");
    }
}
