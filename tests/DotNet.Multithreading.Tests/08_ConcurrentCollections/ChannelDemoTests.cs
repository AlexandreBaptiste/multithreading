using DotNet.Multithreading.Examples.ConcurrentCollections;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.ConcurrentCollections;

public class ChannelDemoTests
{
    private readonly ITestOutputHelper _output;

    public ChannelDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task UnboundedChannelRoundtrip_100Items_SumIsCorrect()
    {
        // Arrange
        const int itemCount = 100;
        int expectedSum = itemCount * (itemCount - 1) / 2; // 0+1+…+99

        // Act
        int sum = await ChannelDemo.UnboundedChannelRoundtrip(itemCount);

        // Assert
        _output.WriteLine($"Channel roundtrip sum: {sum} (expected: {expectedSum})");
        sum.Should().Be(expectedSum, "all items are written and read back from the unbounded channel");
    }

    [Fact]
    public async Task BoundedChannelBackpressure_5Capacity20Items_AllItemsRead()
    {
        // Arrange
        const int capacity = 5;
        const int producerItems = 20;

        // Act
        int count = await ChannelDemo.BoundedChannelBackpressure(capacity, producerItems);

        // Assert
        _output.WriteLine($"Items read: {count} (expected: {producerItems})");
        count.Should().Be(
            producerItems,
            "WriteAsync awaits backpressure rather than dropping items, so all items are eventually read");
    }

    [Fact]
    public async Task BoundedChannelDropOldest_CapacityExceeded_OnlyNewestItemsRemain()
    {
        // Arrange
        // Writes 0,1,2 → full; write 3 drops 0 → [1,2,3]; write 4 drops 1 → [2,3,4]
        const int capacity = 3;
        List<int> expectedItems = Enumerable.Range(2, capacity).ToList();

        // Act
        List<int> items = await ChannelDemo.BoundedChannelDropOldest(capacity);

        // Assert
        _output.WriteLine(
            $"Remaining items: [{string.Join(", ", items)}] " +
            $"(expected: [{string.Join(", ", expectedItems)}])");
        items.Should().Equal(
            expectedItems,
            $"DropOldest discards the two oldest entries, leaving the newest {capacity} items");
    }
}
