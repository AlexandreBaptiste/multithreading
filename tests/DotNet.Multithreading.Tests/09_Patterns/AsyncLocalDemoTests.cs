using DotNet.Multithreading.Examples.Patterns;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DotNet.Multithreading.Tests.Patterns;

public class AsyncLocalDemoTests
{
    private readonly ITestOutputHelper _output;

    public AsyncLocalDemoTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task AsyncLocalFlowsAcrossAwait_ValueSetBeforeAwait_StillAvailableAfter()
    {
        // Act
        string value = await AsyncLocalDemo.AsyncLocalFlowsAcrossAwait();

        // Assert
        _output.WriteLine($"AsyncLocal value after await: '{value}'");
        value.Should().Be("correlation-123",
            "AsyncLocal<T> flows with the logical execution context across await boundaries");
    }

    [Fact]
    public async Task AsyncLocalChildTaskGetsParentValue_ChildReadsParentValue()
    {
        // Act
        string childValue = await AsyncLocalDemo.AsyncLocalChildTaskGetsParentValue();

        // Assert
        _output.WriteLine($"Child task read: '{childValue}'");
        childValue.Should().Be("parent-value",
            "a child Task.Run inherits a snapshot of the parent's AsyncLocal values at creation time");
    }

    [Fact]
    public async Task AsyncLocalChildMutationDoesNotAffectParent_ParentValueIsPreserved()
    {
        // Act
        (string parent, string child) = await AsyncLocalDemo.AsyncLocalChildMutationDoesNotAffectParent();

        // Assert
        _output.WriteLine($"Parent: '{parent}', Child: '{child}'");
        parent.Should().Be("original",
            "each task operates on its own copy of the execution context; child mutations do not propagate back");
        child.Should().Be("mutated",
            "the child task successfully mutated its own copy of the AsyncLocal value");
    }

    [Fact]
    public async Task ThreadLocalDoesNotFlowAcrossAwait_ValueMayBeLostAfterYield()
    {
        // Act
        string? value = await AsyncLocalDemo.ThreadLocalDoesNotFlowAcrossAwait();

        // Assert
        _output.WriteLine($"ThreadLocal value after yield: '{value ?? "<null>"}'");
        // ThreadLocal<T> is thread-scoped: after Task.Yield the continuation may
        // run on a different thread whose slot is null. We document the behavior
        // with a lenient assertion — the value is either null or differs from
        // "thread-value", proving it does not flow across await.
        bool lostOrDifferent = value is null || value != "thread-value";
        lostOrDifferent.Should().BeTrue(
            "ThreadLocal<T> does not flow through await; the value is thread-scoped, not context-scoped");
    }
}
