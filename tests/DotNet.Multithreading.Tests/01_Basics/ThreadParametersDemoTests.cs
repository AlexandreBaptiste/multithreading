using Xunit.Abstractions;
using DotNet.Multithreading.Examples.Basics;

namespace DotNet.Multithreading.Tests.Basics;

/// <summary>
/// Unit tests for <see cref="ThreadParametersDemo"/>.
/// </summary>
public class ThreadParametersDemoTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="ThreadParametersDemoTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public ThreadParametersDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ParameterizedThreadStart_PassesValue_ResultIsCorrect()
    {
        // Arrange
        int input = 21;

        // Act
        int result = ThreadParametersDemo.UsingParameterizedThreadStart(input);

        // Assert
        _output.WriteLine($"Input: {input}, Result: {result}");
        result.Should().Be(42, "the thread receives the input via ParameterizedThreadStart and doubles it");
    }

    [Fact]
    public void LambdaClosure_PassesValue_ResultIsCorrect()
    {
        // Arrange
        int input = 21;

        // Act
        int result = ThreadParametersDemo.UsingLambdaClosure(input);

        // Assert
        _output.WriteLine($"Input: {input}, Result: {result}");
        result.Should().Be(42, "the lambda closure captures the input variable and doubles it");
    }

    [Fact]
    public void StateObject_PassesValue_ResultIsCorrect()
    {
        // Arrange
        int input = 21;

        // Act
        int result = ThreadParametersDemo.UsingStateObject(input);

        // Assert
        _output.WriteLine($"Input: {input}, Result: {result}");
        result.Should().Be(42, "the state object carries the input to the thread; the doubled result is written back");
    }

    [Fact]
    public void ClosureCaptureGotcha_LoopCapture_ValuesAreDuplicated()
    {
        // Arrange — no additional setup required

        // Act
        IReadOnlyList<int> values = ThreadParametersDemo.ClosureCaptureGotcha();

        // Assert — all threads see the *final* value of the loop variable (5)
        _output.WriteLine($"Captured values (gotcha): [{string.Join(", ", values)}]");
        values.Should().HaveCount(5);
        values.Distinct().Count().Should().Be(1,
            "all threads captured i by reference and read its final post-loop value of 5, so every element is identical");
    }

    [Fact]
    public void ClosureCaptureFix_LoopCapture_ValuesAreDistinct()
    {
        // Arrange — no additional setup required

        // Act
        IReadOnlyList<int> values = ThreadParametersDemo.ClosureCaptureFix();

        // Assert — each thread captured its own local copy of i
        _output.WriteLine($"Captured values (fix): [{string.Join(", ", values)}]");
        values.Should().BeEquivalentTo(new[] { 0, 1, 2, 3, 4 },
            "each thread captured a local copy of i, so all five distinct index values are preserved");
    }
}
