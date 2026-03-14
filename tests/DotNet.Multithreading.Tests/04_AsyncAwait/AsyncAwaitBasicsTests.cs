using Xunit.Abstractions;
using DotNet.Multithreading.Examples.AsyncAwait;

namespace DotNet.Multithreading.Tests.AsyncAwait;

/// <summary>
/// Unit tests for <see cref="AsyncAwaitBasics"/>.
/// </summary>
public class AsyncAwaitBasicsTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncAwaitBasicsTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public AsyncAwaitBasicsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ComputeAsync_Completes_ReturnsExpectedValue()
    {
        // Arrange — no additional setup required

        // Act
        int result = await AsyncAwaitBasics.ComputeAsync();

        // Assert
        _output.WriteLine($"ComputeAsync result: {result}");
        result.Should().Be(42, "ComputeAsync awaits a delay and then returns 42");
    }

    [Fact]
    public async Task ComputeSynchronouslyFastPath_FastPath_ReturnsResultWithoutDelay()
    {
        // Arrange — fast == true triggers the zero-allocation ValueTask.FromResult path

        // Act
        int result = await AsyncAwaitBasics.ComputeSynchronouslyFastPath(fast: true);

        // Assert
        _output.WriteLine($"Fast path result: {result}");
        result.Should().Be(42, "the fast path returns ValueTask.FromResult(42) synchronously");
    }

    [Fact]
    public async Task ComputeSynchronouslyFastPath_SlowPath_ReturnsResultAfterDelay()
    {
        // Arrange — fast == false exercises the async state-machine slow path

        // Act
        int result = await AsyncAwaitBasics.ComputeSynchronouslyFastPath(fast: false);

        // Assert
        _output.WriteLine($"Slow path result: {result}");
        result.Should().Be(99, "the slow path awaits Task.Delay and returns 99");
    }

    [Fact]
    public async Task WithConfigureAwaitFalse_Completes_ReturnsTrue()
    {
        // Arrange — no additional setup required

        // Act
        bool result = await AsyncAwaitBasics.WithConfigureAwaitFalse();

        // Assert
        _output.WriteLine($"ConfigureAwait(false) completed: {result}");
        result.Should().BeTrue("WithConfigureAwaitFalse awaits with ConfigureAwait(false) and returns true");
    }

    [Fact]
    public async Task AlreadyCompletedTask_Completes_ReturnsDone()
    {
        // Arrange — no additional setup required

        // Act
        string result = await AsyncAwaitBasics.AlreadyCompletedTask();

        // Assert
        _output.WriteLine($"AlreadyCompletedTask result: {result}");
        result.Should().Be("done", "awaiting Task.CompletedTask is a no-op and the method returns \"done\"");
    }
}
