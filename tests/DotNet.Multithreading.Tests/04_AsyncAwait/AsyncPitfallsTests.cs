using Xunit.Abstractions;
using DotNet.Multithreading.Examples.AsyncAwait;

namespace DotNet.Multithreading.Tests.AsyncAwait;

/// <summary>
/// Unit tests for <see cref="AsyncPitfalls"/>.
/// </summary>
public class AsyncPitfallsTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncPitfallsTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public AsyncPitfallsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SequentialAwaits_TwoTasks50ms_TakesAtLeast100ms()
    {
        // Arrange
        const int delayMs = 50;
        const int count = 2;

        // Act
        long elapsed = await AsyncPitfalls.SequentialAwaits(delayMs, count);

        // Assert
        _output.WriteLine($"Sequential elapsed: {elapsed}ms (expected >= 90ms)");
        elapsed.Should().BeGreaterThanOrEqualTo(90,
            "two sequential 50 ms delays must take at least 90 ms wall-clock time");
    }

    [Fact]
    public async Task ParallelAwaitsFixed_TwoTasks50ms_TakesMuchLessTime()
    {
        // Arrange
        const int delayMs = 50;
        const int count = 2;

        // Act
        long elapsed = await AsyncPitfalls.ParallelAwaitsFixed(delayMs, count);

        // Assert
        _output.WriteLine($"Parallel elapsed: {elapsed}ms (expected < 90ms)");
        elapsed.Should().BeLessThan(90,
            "Task.WhenAll runs both 50 ms delays concurrently so total time should be ~50 ms, well under 90 ms");
    }

    [Fact]
    public async Task AsyncMethodThatNeverYields_ReturnsSynchronously_ValueMatchesExpected()
    {
        // Arrange — no additional setup required

        // Act
        int result = await AsyncPitfalls.AsyncMethodThatNeverYields();

        // Assert
        _output.WriteLine($"AsyncMethodThatNeverYields result: {result}");
        result.Should().Be(0,
            "the method returns ValueTask.FromResult(0) synchronously without yielding");
    }
}
