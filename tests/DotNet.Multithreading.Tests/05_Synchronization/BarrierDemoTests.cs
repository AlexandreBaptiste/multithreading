using DotNet.Multithreading.Examples.Synchronization;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Synchronization;

public class BarrierDemoTests
{
    private readonly ITestOutputHelper _output;

    public BarrierDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PhaseBarrier_4Participants3Phases_CompletesAllPhases()
    {
        // Arrange
        const int participantCount = 4;
        const int phaseCount = 3;

        // Act
        int result = BarrierDemo.PhaseBarrier(participantCount, phaseCount);

        // Assert
        _output.WriteLine($"Completed phases: {result}");
        result.Should().Be(3);
    }

    [Fact]
    public void BarrierParticipantCount_4Participants_Returns4()
    {
        // Arrange
        const int participantCount = 4;

        // Act
        int count = BarrierDemo.BarrierParticipantCount(participantCount);

        // Assert
        _output.WriteLine($"Barrier participant count: {count}");
        count.Should().Be(4);
    }
}
