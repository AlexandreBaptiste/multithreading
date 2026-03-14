using Xunit.Abstractions;
using DotNet.Multithreading.Examples.AsyncAwait;

namespace DotNet.Multithreading.Tests.AsyncAwait;

/// <summary>
/// Unit tests for <see cref="AsyncStreams"/>.
/// </summary>
public class AsyncStreamsTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncStreamsTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public AsyncStreamsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GenerateNumbersAsync_Count5_YieldsExpectedSequence()
    {
        // Arrange
        List<int> collected = [];

        // Act
        await foreach (int n in AsyncStreams.GenerateNumbersAsync(5))
        {
            collected.Add(n);
        }

        // Assert
        _output.WriteLine($"Collected: [{string.Join(", ", collected)}]");
        collected.Should().Equal([0, 1, 2, 3, 4],
            "GenerateNumbersAsync(5) must yield 0, 1, 2, 3, 4 in order");
    }

    [Fact]
    public async Task ConsumeStreamAsync_Count10_ReturnsAllItems()
    {
        // Arrange — no additional setup required

        // Act
        List<int> result = await AsyncStreams.ConsumeStreamAsync(10);

        // Assert
        _output.WriteLine($"Received {result.Count} items");
        result.Should().HaveCount(10, "ConsumeStreamAsync should collect all 10 generated items");
        result.Should().Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
            "values must be the sequence 0..9 in order");
    }

    [Fact]
    public async Task ConsumeWithCancellationAsync_CancelAfter3_ReceivesThreeOrFewerItems()
    {
        // Arrange
        const int totalCount = 100;
        const int cancelAfter = 3;

        // Act
        int received = await AsyncStreams.ConsumeWithCancellationAsync(totalCount, cancelAfter);

        // Assert
        _output.WriteLine($"Items received before cancellation: {received}");
        received.Should().BeLessThanOrEqualTo(cancelAfter,
            "cancellation is requested after cancelAfter items so no more than that many should be received");
        received.Should().BeGreaterThan(0,
            "at least one item must have been received before cancellation");
    }
}
