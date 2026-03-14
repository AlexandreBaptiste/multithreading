using DotNet.Multithreading.Examples.Synchronization;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Synchronization;

public class ReaderWriterLockDemoTests
{
    private readonly ITestOutputHelper _output;

    public ReaderWriterLockDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ConcurrentReads_5Readers_AllReadSimultaneously()
    {
        // Arrange
        const int readerCount = 5;

        // Act
        int peak = ReaderWriterLockDemo.ConcurrentReads(readerCount);

        // Assert
        _output.WriteLine($"Peak concurrent readers: {peak}");
        peak.Should().Be(5);
    }

    [Fact]
    public void ExclusiveWrite_WriterIsBlocked_UntilReadersRelease_ReturnsFalse()
    {
        // Act
        bool acquired = ReaderWriterLockDemo.ExclusiveWrite_WriterIsBlocked_UntilReadersRelease();

        // Assert
        _output.WriteLine($"Write lock acquired while reader active: {acquired}");
        acquired.Should().BeFalse();
    }

    [Fact]
    public void UpgradeableLock_UpgradesSuccessfully_ReturnsTrue()
    {
        // Act
        bool result = ReaderWriterLockDemo.UpgradeableLock();

        // Assert
        _output.WriteLine($"Upgradeable lock cycle completed: {result}");
        result.Should().BeTrue();
    }
}
