using DotNet.Multithreading.Examples.AtomicOperations;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.AtomicOperations;

public class VolatileDemoTests
{
    private readonly ITestOutputHelper _output;

    public VolatileDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TerminateWithVolatileFlag_FlagSet_LoopTerminatesWithinTimeout()
    {
        // Arrange
        int timeoutMs = 2000;

        // Act
        bool terminated = VolatileDemo.TerminateWithVolatileFlag(timeoutMs);

        // Assert
        _output.WriteLine($"Background loop terminated within {timeoutMs} ms: {terminated}");
        terminated.Should().BeTrue("the volatile flag ensures the background thread sees the stop signal");
    }

    [Fact]
    public void VolatileReadWrite_WrittenValue_ReadBackCorrectly()
    {
        // Arrange — no setup needed; method manages its own state

        // Act
        int value = VolatileDemo.VolatileReadWrite();

        // Assert
        _output.WriteLine($"Volatile.Read returned: {value}");
        value.Should().Be(42);
    }
}
