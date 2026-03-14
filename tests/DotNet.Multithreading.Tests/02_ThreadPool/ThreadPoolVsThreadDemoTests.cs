using Xunit.Abstractions;
using DotNet.Multithreading.Examples.ThreadPool;

namespace DotNet.Multithreading.Tests.ThreadPool;

/// <summary>
/// Unit tests for <see cref="ThreadPoolVsThreadDemo"/>.
/// </summary>
public class ThreadPoolVsThreadDemoTests
{
    // Each work item computes 1 + 2 + … + 1 000 = 500 500.
    private const long SumPerItem = 500_500L;
    private const int ItemCount = 100;
    private const long ExpectedTotal = ItemCount * SumPerItem; // 50_050_000

    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="ThreadPoolVsThreadDemoTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public ThreadPoolVsThreadDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RunWithRawThreads_100Items_ProducesCorrectSum()
    {
        // Arrange — ItemCount and ExpectedTotal are class-level constants

        // Act
        long result = ThreadPoolVsThreadDemo.RunWithRawThreads(ItemCount);

        // Assert
        _output.WriteLine($"Raw threads total: {result}");
        result.Should().Be(ExpectedTotal,
            $"each of the {ItemCount} threads computes 1+2+…+1000 = {SumPerItem}");
    }

    [Fact]
    public void RunWithThreadPool_100Items_ProducesCorrectSum()
    {
        // Arrange — ItemCount and ExpectedTotal are class-level constants

        // Act
        long result = ThreadPoolVsThreadDemo.RunWithThreadPool(ItemCount);

        // Assert
        _output.WriteLine($"ThreadPool total: {result}");
        result.Should().Be(ExpectedTotal,
            $"each of the {ItemCount} pool work items computes 1+2+…+1000 = {SumPerItem}");
    }

    [Fact]
    public void BothApproaches_SameCount_ProduceIdenticalResults()
    {
        // Arrange — use the same item count for both approaches
        const int count = 50;

        // Act
        long rawResult = ThreadPoolVsThreadDemo.RunWithRawThreads(count);
        long poolResult = ThreadPoolVsThreadDemo.RunWithThreadPool(count);

        // Assert
        _output.WriteLine($"Raw threads: {rawResult}, ThreadPool: {poolResult}");
        rawResult.Should().Be(poolResult,
            "both approaches perform the same computation and must yield the same total");
    }
}
