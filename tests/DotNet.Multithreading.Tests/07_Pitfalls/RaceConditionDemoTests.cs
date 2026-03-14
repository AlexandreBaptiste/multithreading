using DotNet.Multithreading.Examples.Pitfalls;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Pitfalls;

public class RaceConditionDemoTests
{
    private readonly ITestOutputHelper _output;

    public RaceConditionDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Broken_MultipleThreads_ResultIsLikelyWrong()
    {
        // This test documents that without synchronization the result is non-deterministic.
        // We run 20 iterations and report how many produced an incorrect count.
        // NOTE: This is an intentionally non-deterministic documentation test.
        //       On a single-core machine every run may coincidentally produce the
        //       correct value, so no hard assertion is made.

        // Arrange
        const int threadCount = 10;
        const int incrementsPerThread = 1_000;
        const int correct = threadCount * incrementsPerThread;
        int wrongCount = 0;

        // Act
        for (int i = 0; i < 20; i++)
        {
            int result = RaceConditionDemo.Broken(threadCount, incrementsPerThread);

            if (result != correct)
                wrongCount++;
        }

        // Assert (documentation only — no hard assertion)
        _output.WriteLine($"Wrong results in 20 runs: {wrongCount}/20");
        _output.WriteLine(
            wrongCount > 0
                ? "Race condition observed: at least one run produced a wrong count."
                : "All runs produced the correct count — race condition not observed this time " +
                  "(possible on single-core or very light load).");
    }

    [Fact]
    public void Fixed_MultipleThreads_ResultIsAlwaysCorrect()
    {
        // Arrange
        const int threadCount = 10;
        const int incrementsPerThread = 1_000;
        const int expected = threadCount * incrementsPerThread;

        // Act & Assert — run 20 times to prove determinism
        for (int i = 0; i < 20; i++)
        {
            int result = RaceConditionDemo.Fixed(threadCount, incrementsPerThread);

            _output.WriteLine($"Run {i + 1}: {result}");
            result.Should().Be(expected, because: "Interlocked.Increment is atomic and never loses updates");
        }
    }
}
