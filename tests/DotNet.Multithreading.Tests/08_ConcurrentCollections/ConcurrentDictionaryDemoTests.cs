using DotNet.Multithreading.Examples.ConcurrentCollections;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.ConcurrentCollections;

public class ConcurrentDictionaryDemoTests
{
    private readonly ITestOutputHelper _output;

    public ConcurrentDictionaryDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GetOrAddConcurrently_100Threads_OnlyOneValueWins()
    {
        // Arrange
        const int threadCount = 100;

        // Act
        int value = ConcurrentDictionaryDemo.GetOrAddConcurrently(threadCount);

        // Assert
        _output.WriteLine($"Stored value after {threadCount} concurrent GetOrAdd calls: {value}");
        value.Should().Be(1, "only one factory result wins the race regardless of concurrency");
    }

    [Fact]
    public void AddOrUpdateAccumulator_10Threads10Adds_TotalIs100()
    {
        // Arrange
        const int threadCount = 10;
        const int addsPerThread = 10;

        // Act
        int total = ConcurrentDictionaryDemo.AddOrUpdateAccumulator(threadCount, addsPerThread);

        // Assert
        _output.WriteLine($"Accumulated total: {total} (expected: {threadCount * addsPerThread})");
        total.Should().Be(
            threadCount * addsPerThread,
            "each AddOrUpdate call atomically increments the counter exactly once");
    }

    [Fact]
    public void TryAddTryRemoveConcurrently_100Items_AllRemoved()
    {
        // Arrange
        const int itemCount = 100;

        // Act
        int removed = ConcurrentDictionaryDemo.TryAddTryRemoveConcurrently(itemCount);

        // Assert
        _output.WriteLine($"Items removed: {removed} out of {itemCount}");
        removed.Should().Be(itemCount, "every added item should be successfully removed");
    }
}
