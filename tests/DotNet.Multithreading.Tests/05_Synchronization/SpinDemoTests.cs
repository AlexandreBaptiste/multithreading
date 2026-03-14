using DotNet.Multithreading.Examples.Synchronization;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Synchronization;

public class SpinDemoTests
{
    private readonly ITestOutputHelper _output;

    public SpinDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SpinLockExample_10Threads_CountIsCorrect()
    {
        // Arrange
        const int threadCount = 10;

        // Act
        int result = SpinDemo.SpinLockExample(threadCount);

        // Assert
        _output.WriteLine($"Final counter: {result}");
        result.Should().Be(10);
    }

    [Fact]
    public void SpinWaitExample_FlagSetFromOtherThread_ReturnsTrue()
    {
        // Act
        bool result = SpinDemo.SpinWaitExample();

        // Assert
        _output.WriteLine($"SpinWait received signal: {result}");
        result.Should().BeTrue();
    }
}
