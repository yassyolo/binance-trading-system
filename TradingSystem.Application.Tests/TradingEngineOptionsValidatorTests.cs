using TradingSystem.Application.Engine.Configuration;
using Xunit;

namespace TradingSystem.Application.Tests;

public sealed class TradingEngineOptionsValidatorTests
{
    private readonly TradingEngineOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        Assert.True(_sut.Validate(null, new TradingEngineOptions()).Succeeded);
    }

    [Fact]
    public void Validate_ZeroProcessingTtl_Fails()
    {
        var options = new TradingEngineOptions { ProcessingIdempotencyTtl = TimeSpan.Zero };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_CompletedTtlShorterThanProcessing_Fails()
    {
        var options = new TradingEngineOptions { ProcessingIdempotencyTtl = TimeSpan.FromMinutes(2), CompletedIdempotencyTtl = TimeSpan.FromMinutes(1) };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_ZeroOperationLockTtl_Fails()
    {
        var options = new TradingEngineOptions { OperationLockTtl = TimeSpan.Zero };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NegativeMaximumSignalAge_Fails()
    {
        var options = new TradingEngineOptions { MaximumSignalAge = TimeSpan.FromSeconds(-1) };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NegativeFutureClockSkew_Fails()
    {
        var options = new TradingEngineOptions { MaximumFutureClockSkew = TimeSpan.FromSeconds(-1) };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NonPositiveRetryCount_Fails()
    {
        var options = new TradingEngineOptions { PostExecutionCompletionRetryCount = 0 };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }

    [Fact]
    public void Validate_NegativeRetryDelay_Fails()
    {
        var options = new TradingEngineOptions { PostExecutionCompletionRetryDelay = TimeSpan.FromMilliseconds(-1) };
        Assert.False(_sut.Validate(null, options).Succeeded);
    }
}
