using DotNet.Multithreading.Examples.Patterns;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Patterns;

public class ProducerConsumerPatternTests
{
    private readonly ITestOutputHelper _output;

    public ProducerConsumerPatternTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task RunAsync_2Producers2Consumers10ItemsEach_SumIsCorrect()
    {
        // Arrange
        const int producers = 2;
        const int consumers = 2;
        const int itemsPerProducer = 10;
        // Each producer writes 1+2+…+10 = 55; two producers → 110
        const long expectedSum = 110L;

        // Act
        long total = await ProducerConsumerPattern.RunAsync(producers, consumers, itemsPerProducer);

        // Assert
        _output.WriteLine($"Total consumed: {total} (expected: {expectedSum})");
        total.Should().Be(expectedSum, "each producer writes 1..10 and both sums equal 55 each");
    }

    [Fact]
    public async Task RunAsync_WithCancellation_TaskIsCancelled()
    {
        // Arrange — pre-cancel the token so the pipeline is cancelled deterministically
        // when WriteAsync is first called (rather than relying on a time-based race).
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act
        Func<Task> act = async () =>
            await ProducerConsumerPattern.RunAsync(
                producerCount: 4,
                consumerCount: 4,
                itemsPerProducer: 10_000,
                ct: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>(
            "the token is already cancelled before any items are written");
    }
}
