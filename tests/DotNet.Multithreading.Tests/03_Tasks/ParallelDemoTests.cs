using Xunit.Abstractions;
using DotNet.Multithreading.Examples.Tasks;

namespace DotNet.Multithreading.Tests.Tasks;

/// <summary>
/// Unit tests for <see cref="ParallelDemo"/>.
/// </summary>
public class ParallelDemoTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="ParallelDemoTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public ParallelDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParallelForSum_1000_EqualsSequentialSum()
    {
        // Arrange
        int n = 1000;
        int expectedSum = n * (n + 1) / 2; // 500500

        // Act
        int result = ParallelDemo.ParallelForSum(n);

        // Assert
        _output.WriteLine($"Parallel.For sum(1..{n}): {result}");
        result.Should().Be(expectedSum,
            "Interlocked.Add ensures thread-safe accumulation; result matches the arithmetic formula");
    }

    [Fact]
    public void ParallelForEachSum_Values_EqualsExpectedSum()
    {
        // Arrange
        int[] values = new[] { 1, 2, 3, 4, 5 };
        int expectedSum = 15;

        // Act
        int result = ParallelDemo.ParallelForEachSum(values);

        // Assert
        _output.WriteLine($"Parallel.ForEach sum: {result}");
        result.Should().Be(expectedSum,
            "Interlocked.Add ensures each value is counted exactly once regardless of thread order");
    }

    [Fact]
    public void ParallelForEachWithMaxDegree_LimitsParallelism_ProducesCorrectSum()
    {
        // Arrange
        int[] values = new[] { 1, 2, 3, 4, 5 };
        int maxDegree = 2;
        int expectedSum = 15;

        // Act
        int result = ParallelDemo.ParallelForEachWithMaxDegree(values, maxDegree);

        // Assert
        _output.WriteLine($"Parallel.ForEach (maxDegree={maxDegree}) sum: {result}");
        result.Should().Be(expectedSum,
            "capping concurrency via MaxDegreeOfParallelism does not change the final sum");
    }

    [Fact]
    public void PlinqOrderedSum_1000_EqualsArithmeticSum()
    {
        // Arrange
        int n = 1000;
        int expectedSum = n * (n + 1) / 2; // 500500

        // Act
        int result = ParallelDemo.PlinqOrderedSum(n);

        // Assert
        _output.WriteLine($"PLINQ ordered sum(1..{n}): {result}");
        result.Should().Be(expectedSum,
            "AsOrdered().Select(x => x).Sum() preserves all values; result equals the arithmetic formula");
    }

    [Fact]
    public void PlinqWithCancellation_TokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Action act = () => ParallelDemo.PlinqWithCancellation(1000, cts.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>(
            "WithCancellation propagates an already-cancelled token as OperationCanceledException");
    }
}
