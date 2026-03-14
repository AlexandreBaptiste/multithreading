using DotNet.Multithreading.Examples.Pitfalls;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Pitfalls;

public class LivelockDemoTests
{
    private readonly ITestOutputHelper _output;

    public LivelockDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Broken_BothThreadsYield_LivelockReachesMaxIterations()
    {
        // Arrange
        const int maxIterations = 100;

        // Act
        int result = LivelockDemo.Broken(maxIterations);

        // Assert
        _output.WriteLine($"Iterations reached before giving up: {result}");
        result.Should().Be(maxIterations,
            because: "both threads always back off symmetrically; they exhaust the retry " +
                     "budget without completing any real work — the observable sign of livelock");
    }

    [Fact]
    public void Fixed_RandomizedBackoff_CompletesSuccessfully()
    {
        // Arrange
        const int iterations = 10;

        // Act
        int completed = LivelockDemo.Fixed(iterations);

        // Assert
        _output.WriteLine($"Completed: {completed}");
        completed.Should().Be(iterations,
            because: "randomised back-off breaks the symmetry; both threads eventually " +
                     "proceed and complete all work items");
    }
}
