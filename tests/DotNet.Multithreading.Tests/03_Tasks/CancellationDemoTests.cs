using Xunit.Abstractions;
using DotNet.Multithreading.Examples.Tasks;

namespace DotNet.Multithreading.Tests.Tasks;

/// <summary>
/// Unit tests for <see cref="CancellationDemo"/>.
/// </summary>
public class CancellationDemoTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="CancellationDemoTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public CancellationDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CancelAfterDelay_TokenCancelled_ReturnsCancelledString()
    {
        // Arrange
        int cancelAfterMs = 50;

        // Act
        string result = await CancellationDemo.CancelAfterDelay(cancelAfterMs);

        // Assert
        _output.WriteLine($"Result after {cancelAfterMs} ms cancellation: {result}");
        result.Should().Be("cancelled",
            "the loop exits via OperationCanceledException and returns the 'cancelled' sentinel");
    }

    [Fact]
    public void LinkedTokenSources_OneParentCancelled_LinkedTokenIsCancelled()
    {
        // Arrange — method creates two CTS instances and links them

        // Act
        bool linkedIsCancelled = CancellationDemo.LinkedTokenSources();

        // Assert
        _output.WriteLine($"Linked token IsCancellationRequested: {linkedIsCancelled}");
        linkedIsCancelled.Should().BeTrue(
            "cancelling any parent source propagates immediately to the linked token");
    }

    [Fact]
    public void CooperativeCancellation_TokenCancelled_RegisterCallbackFired()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        bool callbackFired = CancellationDemo.CooperativeCancellation(cts.Token);

        // Assert
        _output.WriteLine($"Callback fired: {callbackFired}");
        callbackFired.Should().BeTrue(
            "registering a callback on an already-cancelled token fires it synchronously before Register returns");
    }
}
