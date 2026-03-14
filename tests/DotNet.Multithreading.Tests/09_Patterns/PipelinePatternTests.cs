using DotNet.Multithreading.Examples.Patterns;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Patterns;

public class PipelinePatternTests
{
    private readonly ITestOutputHelper _output;

    public PipelinePatternTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RunAsync_5Items_ProducesCorrectTransformedOutput()
    {
        // Arrange
        const int itemCount = 5;
        List<string> expected = ["item-1", "item-2", "item-3", "item-4", "item-5"];

        // Act
        List<string> result = await PipelinePattern.RunAsync(itemCount);

        // Assert
        _output.WriteLine($"Results: {string.Join(", ", result)}");
        result.Should().HaveCount(5, "one output per produced integer");
        result.Should().Contain("item-3", "item-3 is produced from integer 3");
        result.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering(),
            "transformation is 'item-N' and order must be preserved through the pipeline");
    }

    [Fact]
    public async Task RunAsync_100Items_AllItemsProcessed()
    {
        // Arrange
        const int itemCount = 100;

        // Act
        List<string> result = await PipelinePattern.RunAsync(itemCount);

        // Assert
        _output.WriteLine($"Total items processed: {result.Count}");
        result.Should().HaveCount(itemCount, "every produced integer must pass through all three stages");
        result.Should().Contain("item-1");
        result.Should().Contain("item-100");
    }
}
