using DotNet.Multithreading.Examples.AtomicOperations;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.AtomicOperations;

public class InterlockedDemoTests
{
    private readonly ITestOutputHelper _output;

    public InterlockedDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void IncrementConcurrently_10Threads1000Increments_TotalIs10000()
    {
        // Arrange
        int threadCount = 10;
        int incrementsPerThread = 1000;

        // Act
        int total = InterlockedDemo.IncrementConcurrently(threadCount, incrementsPerThread);

        // Assert
        _output.WriteLine($"Total after {threadCount} threads × {incrementsPerThread} increments = {total}");
        total.Should().Be(threadCount * incrementsPerThread);
    }

    [Fact]
    public void DecrementToZero_100Threads_ResultIsZero()
    {
        // Arrange
        int startValue = 100;

        // Act
        int result = InterlockedDemo.DecrementToZero(startValue);

        // Assert
        _output.WriteLine($"Counter after {startValue} decrements = {result}");
        result.Should().Be(0);
    }

    [Fact]
    public void ExchangeExample_ReturnsOldValue()
    {
        // Arrange
        int initial = 5;
        int newValue = 99;

        // Act
        int oldValue = InterlockedDemo.ExchangeExample(initial, newValue);

        // Assert
        _output.WriteLine($"Exchange({initial} → {newValue}): returned old value = {oldValue}");
        oldValue.Should().Be(initial);
    }

    [Fact]
    public void CompareExchangeExample_ComparandMatches_SwapsValue()
    {
        // Arrange
        int initial = 10;
        int comparand = 10;
        int newValue = 99;

        // Act
        (int result, int observedOld) = InterlockedDemo.CompareExchangeExample(initial, comparand, newValue);

        // Assert
        _output.WriteLine($"CAS({initial}, comparand={comparand}, new={newValue}): observedOld={observedOld}, result={result}");
        observedOld.Should().Be(initial, "CompareExchange returns the old value");
        result.Should().Be(newValue, "swap should have occurred because comparand matched");
    }

    [Fact]
    public void CompareExchangeExample_ComparandDoesNotMatch_DoesNotSwap()
    {
        // Arrange
        int initial = 10;
        int comparand = 5;  // does NOT match initial
        int newValue = 99;

        // Act
        (int result, int observedOld) = InterlockedDemo.CompareExchangeExample(initial, comparand, newValue);

        // Assert
        _output.WriteLine($"CAS({initial}, comparand={comparand}, new={newValue}): observedOld={observedOld}, result={result}");
        observedOld.Should().Be(initial, "CompareExchange always returns the old value");
        result.Should().Be(initial, "no swap should have occurred because comparand did not match");
    }

    [Fact]
    public void LockFreeCounterCasLoop_10Threads100Increments_TotalIs1000()
    {
        // Arrange
        int threadCount = 10;
        int incrementsPerThread = 100;

        // Act
        int total = InterlockedDemo.LockFreeCounterCasLoop(threadCount, incrementsPerThread);

        // Assert
        _output.WriteLine($"CAS-loop total after {threadCount} threads × {incrementsPerThread} increments = {total}");
        total.Should().Be(threadCount * incrementsPerThread);
    }
}
