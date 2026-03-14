using Xunit.Abstractions;
using DotNet.Multithreading.Examples.Tasks;

namespace DotNet.Multithreading.Tests.Tasks;

/// <summary>
/// Unit tests for <see cref="TaskCombinators"/>.
/// </summary>
public class TaskCombinatorsTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="TaskCombinatorsTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public TaskCombinatorsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task WhenAll_MultipleTasks_CollectsAllResults()
    {
        // Arrange
        int[] input = new[] { 1, 2, 3 };
        int[] expected = new[] { 2, 4, 6 };

        // Act
        int[] result = await TaskCombinators.WhenAllExample(input);

        // Assert
        _output.WriteLine($"WhenAll results: [{string.Join(", ", result)}]");
        result.Should().Equal(expected,
            "each value is doubled; Task.WhenAll preserves input order");
    }

    [Fact]
    public async Task WhenAny_MultipleTasks_ReturnsFirstCompleted()
    {
        // Arrange — three tasks with 10 ms, 50 ms, and 100 ms delays

        // Act
        int firstIndex = await TaskCombinators.WhenAnyExample();

        // Assert
        _output.WriteLine($"First completed task returned: {firstIndex}");
        firstIndex.Should().Be(0,
            "the 10 ms task completes first and returns value 0");
    }

    [Fact]
    public async Task WhenAllWithPartialFailure_TwoSucceedOneFails_AggregateExceptionContainsOneInner()
    {
        // Arrange — method internally starts 3 tasks, 1 of which throws

        // Act
        int innerCount = await TaskCombinators.WhenAllWithPartialFailure();

        // Assert
        _output.WriteLine($"AggregateException inner exception count: {innerCount}");
        innerCount.Should().Be(1,
            "exactly one of the three tasks throws, so the AggregateException has one inner exception");
    }

    [Fact]
    public void WaitAllBlocking_MultipleTasks_SumIsCorrect()
    {
        // Arrange
        int count = 5;
        int expectedSum = 15; // 1+2+3+4+5

        // Act
        int result = TaskCombinators.WaitAllBlocking(count);

        // Assert
        _output.WriteLine($"WaitAll sum for count={count}: {result}");
        result.Should().Be(expectedSum,
            "Task.WaitAll blocks until all tasks complete, then summing 1..5 gives 15");
    }
}
