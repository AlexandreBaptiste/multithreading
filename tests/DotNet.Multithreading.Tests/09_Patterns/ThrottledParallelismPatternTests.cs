using DotNet.Multithreading.Examples.Patterns;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Patterns;

public class ThrottledParallelismPatternTests
{
    private readonly ITestOutputHelper _output;

    public ThrottledParallelismPatternTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ProcessWithThrottle_10Items3Concurrent_AllResultsCorrect()
    {
        // Arrange
        int[] items = Enumerable.Range(1, 10).ToArray();
        int[] expected = items.Select(x => x * 2).ToArray();

        // Act
        int[] results = await ThrottledParallelismPattern.ProcessWithThrottle(
            items: items,
            maxConcurrent: 3,
            processor: async item =>
            {
                await Task.Delay(10);
                return item * 2;
            });

        // Assert
        _output.WriteLine($"Results: {string.Join(", ", results)}");
        results.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering(),
            "Task.WhenAll preserves input order and every item is doubled");
    }

    [Fact]
    public async Task MeasurePeakConcurrency_20Items4MaxConcurrent_PeakIsAtMost4()
    {
        // Arrange
        const int totalItems = 20;
        const int maxConcurrent = 4;

        // Act
        int peak = await ThrottledParallelismPattern.MeasurePeakConcurrency(totalItems, maxConcurrent);

        // Assert
        _output.WriteLine($"Peak concurrency observed: {peak} (max allowed: {maxConcurrent})");
        peak.Should().BeGreaterThan(0, "at least one item must be processed");
        peak.Should().BeLessThanOrEqualTo(maxConcurrent,
            "SemaphoreSlim(maxConcurrent) ensures no more than maxConcurrent tasks execute simultaneously");
    }
}
