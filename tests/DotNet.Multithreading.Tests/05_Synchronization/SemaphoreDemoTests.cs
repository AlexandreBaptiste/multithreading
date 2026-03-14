using DotNet.Multithreading.Examples.Synchronization;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Synchronization;

public class SemaphoreDemoTests
{
    private readonly ITestOutputHelper _output;

    public SemaphoreDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ThrottleConcurrency_20Tasks4Max_PeakNeverExceedsMax()
    {
        // Arrange
        const int totalTasks = 20;
        const int maxConcurrent = 4;

        // Act
        int peak = await SemaphoreDemo.ThrottleConcurrency(totalTasks, maxConcurrent);

        // Assert
        _output.WriteLine($"Peak concurrent tasks: {peak} (max allowed: {maxConcurrent})");
        peak.Should().BeLessThanOrEqualTo(maxConcurrent);
    }

    [Fact]
    public async Task WaitAsyncExample_SlotAvailable_AcquiresSuccessfully()
    {
        // Arrange
        const int initialCount = 1;

        // Act
        bool result = await SemaphoreDemo.WaitAsyncExample(initialCount);

        // Assert
        result.Should().BeTrue();
    }
}
