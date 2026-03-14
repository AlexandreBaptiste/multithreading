using Xunit.Abstractions;
using DotNet.Multithreading.Examples.Basics;

namespace DotNet.Multithreading.Tests.Basics;

/// <summary>
/// Unit tests for <see cref="ThreadFundamentals"/>.
/// </summary>
public class ThreadFundamentalsTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Initializes a new instance of <see cref="ThreadFundamentalsTests"/>.
    /// </summary>
    /// <param name="output">xUnit output helper injected by the test runner.</param>
    public ThreadFundamentalsTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RunAndJoin_ThreadDoesWork_ResultIsCorrect()
    {
        // Arrange — no additional setup required

        // Act
        int result = ThreadFundamentals.RunAndJoin();

        // Assert
        _output.WriteLine($"Counter after Join: {result}");
        result.Should().Be(1, "the worker thread increments the counter exactly once before Join returns");
    }

    [Fact]
    public void GetThreadName_NameIsSet_NamePreservedInsideThread()
    {
        // Arrange
        string expectedName = "TestWorker";

        // Act
        string? observedName = ThreadFundamentals.GetThreadName(expectedName);

        // Assert
        _output.WriteLine($"Name observed inside thread: {observedName}");
        observedName.Should().Be(expectedName,
            "Thread.CurrentThread.Name inside the thread should equal the name assigned before Start");
    }

    [Fact]
    public void IsBackground_NewThread_DefaultIsFalse()
    {
        // Arrange — no additional setup required

        // Act
        bool isBackground = ThreadFundamentals.IsBackgroundDefault();

        // Assert
        _output.WriteLine($"IsBackground (default): {isBackground}");
        isBackground.Should().BeFalse(
            "new Thread() creates a foreground thread by default; IsBackground is false");
    }

    [Fact]
    public void ThreadState_BeforeStart_IsUnstarted()
    {
        // Arrange — no additional setup required

        // Act
        ThreadState state = ThreadFundamentals.GetThreadStateBeforeStart();

        // Assert
        _output.WriteLine($"ThreadState before Start(): {state}");
        state.Should().Be(ThreadState.Unstarted,
            "a thread that has never been started is always in the Unstarted state");
    }

    [Fact]
    public void ThreadState_AfterJoin_IsStopped()
    {
        // Arrange — no additional setup required

        // Act
        ThreadState state = ThreadFundamentals.GetThreadStateAfterJoin();

        // Assert
        _output.WriteLine($"ThreadState after Join(): {state}");
        state.Should().Be(ThreadState.Stopped,
            "once a thread's delegate returns and Join() completes, the thread is in the Stopped state");
    }
}
