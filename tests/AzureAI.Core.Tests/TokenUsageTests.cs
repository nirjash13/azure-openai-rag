using AzureAI.Core.Domain.ValueObjects;
using FluentAssertions;

namespace AzureAI.Core.Tests;

public sealed class TokenUsageTests
{
    [Fact]
    public void Calculate_TotalTokens_EqualsSumOfPromptAndCompletion()
    {
        var usage = TokenUsage.Calculate(100, 50, 0.001m, 0.002m);
        usage.TotalTokens.Should().Be(150);
    }

    [Fact]
    public void Calculate_EstimatedCost_UsesCorrectRates()
    {
        var usage = TokenUsage.Calculate(100, 50, 0.001m, 0.002m);
        usage.EstimatedCost.Should().Be(100 * 0.001m + 50 * 0.002m);
    }

    [Fact]
    public void Calculate_ZeroTokens_ZeroCost()
    {
        var usage = TokenUsage.Calculate(0, 0, 0.001m, 0.002m);
        usage.TotalTokens.Should().Be(0);
        usage.EstimatedCost.Should().Be(0m);
    }

    [Fact]
    public void Record_SameValues_AreEqual()
    {
        var a = new TokenUsage(10, 20, 0.05m);
        var b = new TokenUsage(10, 20, 0.05m);
        a.Should().Be(b);
    }
}
