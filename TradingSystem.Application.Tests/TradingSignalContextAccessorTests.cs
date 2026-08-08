using TradingSystem.Application.Execution;
using TradingSystem.Application.Execution.Models;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class TradingSignalContextAccessorTests
{
    [Fact]
    public void Push_SetsCurrentUntilDisposed()
    {
        var sut = new TradingSignalContextAccessor();
        var context = new TradingSignalExecutionContext("s1", "1.0", "test");

        using (sut.Push(context))
            Assert.Same(context, sut.Current);

        Assert.Null(sut.Current);
    }

    [Fact]
    public void Push_NestedScope_RestoresPreviousContext()
    {
        var sut = new TradingSignalContextAccessor();
        var outer = new TradingSignalExecutionContext("outer", "1.0", "test");
        var inner = new TradingSignalExecutionContext("inner", "1.0", "test");

        using (sut.Push(outer))
        {
            using (sut.Push(inner))
                Assert.Same(inner, sut.Current);

            Assert.Same(outer, sut.Current);
        }

        Assert.Null(sut.Current);
    }

    [Fact]
    public void Push_Null_Throws()
    {
        var sut = new TradingSignalContextAccessor();
        Assert.Throws<ArgumentNullException>(() => sut.Push(null!));
    }
}
