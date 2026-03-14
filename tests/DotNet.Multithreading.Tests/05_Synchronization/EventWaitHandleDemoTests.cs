using DotNet.Multithreading.Examples.Synchronization;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Synchronization;

public class EventWaitHandleDemoTests
{
    private readonly ITestOutputHelper _output;

    public EventWaitHandleDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ManualResetEventUnblocksAll_5Waiters_AllUnblocked()
    {
        // Arrange
        const int waiterCount = 5;

        // Act
        int result = EventWaitHandleDemo.ManualResetEventUnblocksAll(waiterCount);

        // Assert
        _output.WriteLine($"Unblocked threads: {result}");
        result.Should().Be(5);
    }

    [Fact]
    public void AutoResetEventUnblocksOne_5Attempts_ExactlyOneSucceeds()
    {
        // Arrange
        const int attemptCount = 5;

        // Act
        int result = EventWaitHandleDemo.AutoResetEventUnblocksOne(attemptCount);

        // Assert
        _output.WriteLine($"Threads that received the signal: {result}");
        result.Should().Be(1);
    }

    [Fact]
    public void CountdownEventSignalsWhenDone_5Participants_WaitReturnsTrue()
    {
        // Arrange
        const int participantCount = 5;

        // Act
        bool result = EventWaitHandleDemo.CountdownEventSignalsWhenDone(participantCount);

        // Assert
        _output.WriteLine($"CountdownEvent completed within timeout: {result}");
        result.Should().BeTrue();
    }
}
