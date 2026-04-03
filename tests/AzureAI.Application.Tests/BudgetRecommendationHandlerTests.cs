using AzureAI.Application.Queries.Recommendations;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class BudgetRecommendationHandlerTests
{
    private readonly Mock<ICompletionService> _completionService = new();
    private readonly Mock<ILogger<BudgetRecommendationHandler>> _logger = new();

    private BudgetRecommendationHandler CreateHandler() =>
        new(_completionService.Object, _logger.Object);

    private void SetupCompletion(string content)
    {
        var usage = new TokenUsage(10, 20, 0.01m);
        _completionService
            .Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(content, usage));
    }

    [Fact]
    public async Task Handle_ValidJson_ReturnsRecommendations()
    {
        const string json = """
            [
              {"category":"Travel","recommendation":"Use economy class for domestic trips","potential_savings":500,"priority":"Medium","rationale":"Business travel costs are elevated."}
            ]
            """;
        SetupCompletion(json);

        var spending = new List<SpendingCategory>
        {
            new("Travel", 3000m, 2000m),
            new("Office",  500m,  600m),
        };

        var handler = CreateHandler();
        var result  = await handler.Handle(new BudgetRecommendationQuery(spending, TotalBudget: 5000m), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Travel");
        result[0].PotentialSavings.Should().Be(500m);
        result[0].Priority.Should().Be("Medium");
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsEmptyList()
    {
        SetupCompletion("Sorry, I cannot help with that.");

        var spending = new List<SpendingCategory> { new("Food", 1000m) };

        var handler = CreateHandler();
        var result  = await handler.Handle(new BudgetRecommendationQuery(spending), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
