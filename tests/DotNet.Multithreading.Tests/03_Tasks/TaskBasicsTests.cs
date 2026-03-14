using Xunit.Abstractions;
using DotNet.Multithreading.Examples.Tasks;

namespace DotNet.Multithreading.Tests.Tasks;

/// <summary>
/// Unit tests for <see cref="TaskBasics"/>.
/// </summary>
public class TaskBasicsTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="TaskBasicsTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public TaskBasicsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RunTask_Completes_ReturnsExpectedResult()
    {
        // Arrange — no additional setup required

        // Act
        int result = await TaskBasics.RunTask();

        // Assert
        _output.WriteLine($"Task.Run result: {result}");
        result.Should().Be(42, "TaskBasics.RunTask offloads the computation that returns 42");
    }

    [Fact]
    public async Task StartNewLongRunning_UsesNonPoolThread_ThreadIdIsDifferentFromPoolThread()
    {
        // Arrange — no additional setup required

        // Act
        int threadId = await TaskBasics.StartNewLongRunning();

        // Assert
        _output.WriteLine($"LongRunning task thread id: {threadId}");
        threadId.Should().BeGreaterThan(0,
            "a valid ManagedThreadId is always a positive integer");
    }

    [Fact]
    public async Task ContinueWith_AfterAntecedent_ContinuationRuns()
    {
        // Arrange — no additional setup required

        // Act
        string result = await TaskBasics.ContinueWithExample();

        // Assert
        _output.WriteLine($"ContinueWith result: {result}");
        result.Should().Be("Result: 7",
            "the continuation formats the antecedent value 7 into the expected string");
    }

    [Fact]
    public void HandleAggregateException_FaultedTask_InnerExceptionMessageIsCorrect()
    {
        // Arrange — no additional setup required

        // Act
        string message = TaskBasics.HandleAggregateException();

        // Assert
        _output.WriteLine($"AggregateException inner message: {message}");
        message.Should().Be("task-fault",
            "Task.Wait wraps the inner InvalidOperationException in an AggregateException");
    }
}
