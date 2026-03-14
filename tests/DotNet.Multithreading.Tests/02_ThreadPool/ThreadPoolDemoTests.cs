using Xunit.Abstractions;
using DotNet.Multithreading.Examples.ThreadPool;

namespace DotNet.Multithreading.Tests.ThreadPool;

/// <summary>
/// Unit tests for <see cref="ThreadPoolDemo"/>.
/// </summary>
public class ThreadPoolDemoTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="ThreadPoolDemoTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public ThreadPoolDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void QueueWorkItems_AllItemsComplete_CountMatchesExpected()
    {
        // Arrange
        const int expected = 50;

        // Act
        int actual = ThreadPoolDemo.QueueWorkItems(expected);

        // Assert
        _output.WriteLine($"Work items executed: {actual}");
        actual.Should().Be(expected,
            "every queued work item must execute exactly once before QueueWorkItems returns");
    }

    [Fact]
    public void QueueWorkItemWithState_StateIsPassed_EchoedCorrectly()
    {
        // Arrange
        const string payload = "hello";

        // Act
        string echo = ThreadPoolDemo.QueueWorkItemWithState(payload);

        // Assert
        _output.WriteLine($"Echoed state: {echo}");
        echo.Should().Be(payload,
            "the state object passed to QueueUserWorkItem must be forwarded unchanged to the callback");
    }

    [Fact]
    public void GetMinMaxThreads_ReturnsPositiveValues_ThreadCountsAreReasonable()
    {
        // Arrange — no additional setup required

        // Act
        (int minWorker, int minIO, int maxWorker, int maxIO) = ThreadPoolDemo.GetMinMaxThreads();

        // Assert
        _output.WriteLine($"Min workers: {minWorker}, Min IO: {minIO}, Max workers: {maxWorker}, Max IO: {maxIO}");
        minWorker.Should().BePositive("the pool always has at least one minimum worker thread");
        minIO.Should().BePositive("the pool always has at least one minimum I/O thread");
        maxWorker.Should().BeGreaterThanOrEqualTo(minWorker,
            "the maximum worker count must be at least as large as the minimum");
        maxIO.Should().BeGreaterThanOrEqualTo(minIO,
            "the maximum I/O count must be at least as large as the minimum");
    }

    [Fact]
    public void RegisterWaitCallback_EventSignaled_CallbackFired()
    {
        // Arrange
        const int timeoutMs = 5_000;

        // Act
        bool callbackFired = ThreadPoolDemo.RegisterWaitCallback(timeoutMs);

        // Assert
        _output.WriteLine($"Callback fired without timeout: {callbackFired}");
        callbackFired.Should().BeTrue(
            "the AutoResetEvent is signalled immediately, so the registered callback must fire before the timeout");
    }
}
