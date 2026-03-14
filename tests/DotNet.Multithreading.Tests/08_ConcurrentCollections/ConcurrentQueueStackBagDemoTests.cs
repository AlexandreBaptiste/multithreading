using DotNet.Multithreading.Examples.ConcurrentCollections;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.ConcurrentCollections;

public class ConcurrentQueueStackBagDemoTests
{
    private readonly ITestOutputHelper _output;

    public ConcurrentQueueStackBagDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void QueueFifoOrder_5Items_DequeueOrderMatchesEnqueueOrder()
    {
        // Arrange
        const int itemCount = 5;

        // Act
        List<int> items = ConcurrentQueueStackBagDemo.QueueFifoOrder(itemCount);

        // Assert
        _output.WriteLine($"Dequeued: [{string.Join(", ", items)}]");
        items.Should().Equal(new List<int> { 0, 1, 2, 3, 4 }, "ConcurrentQueue is FIFO — dequeue order mirrors enqueue order");
    }

    [Fact]
    public void StackLifoOrder_5Items_PopOrderIsReversed()
    {
        // Arrange
        const int itemCount = 5;

        // Act
        List<int> items = ConcurrentQueueStackBagDemo.StackLifoOrder(itemCount);

        // Assert
        _output.WriteLine($"Popped: [{string.Join(", ", items)}]");
        items.Should().Equal(new List<int> { 4, 3, 2, 1, 0 }, "ConcurrentStack is LIFO — pop order is the reverse of push order");
    }

    [Fact]
    public void BagMultipleProducers_5Producers10Items_TotalItemCountIs50()
    {
        // Arrange
        const int producerCount = 5;
        const int itemsPerProducer = 10;

        // Act
        int total = ConcurrentQueueStackBagDemo.BagMultipleProducers(producerCount, itemsPerProducer);

        // Assert
        _output.WriteLine($"Total items in bag: {total} (expected: {producerCount * itemsPerProducer})");
        total.Should().Be(
            producerCount * itemsPerProducer,
            "each producer adds its full share of items to the bag");
    }
}
