using AzureAI.Application.Queries.Forecast;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class ForecastHandlerTests
{
    private readonly Mock<ICompletionService> _completionService = new();
    private readonly Mock<ILogger<ForecastHandler>> _logger = new();

    private ForecastHandler CreateHandler() =>
        new(_completionService.Object, _logger.Object);

    private void SetupCompletion(string content)
    {
        var usage = new TokenUsage(10, 20, 30, 0.01m);
        _completionService
            .Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(content, usage));
    }

    [Fact]
    public async Task Handle_ValidJson_ReturnsForecastList()
    {
        const string json = """
            [
              {"period":"2024-Q2","projected_amount":2000,"lower_bound":1800,"upper_bound":2200,"confidence_level":0.9,"explanation":"Upward trend"}
            ]
            """;
        SetupCompletion(json);

        var handler = CreateHandler();
        var query = new ForecastQuery(
            [new HistoricalPeriod("2024-Q1", 1800m)],
            PeriodsToForecast: 1);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Period.Should().Be("2024-Q2");
        result[0].ProjectedAmount.Should().Be(2000m);
        result[0].ConfidenceLevel.Should().Be(0.9);
        result[0].Explanation.Should().Be("Upward trend");
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsFallback()
    {
        SetupCompletion("this is not json at all");

        var handler = CreateHandler();
        var query = new ForecastQuery([new HistoricalPeriod("2024-Q1", 1500m)]);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ProjectedAmount.Should().Be(0m);
        result[0].Explanation.Should().Be("Forecast unavailable.");
    }

    [Fact]
    public async Task Handle_EmptyHistoricalData_ReturnsFallbackWithoutCallingCompletion()
    {
        var handler = CreateHandler();
        var query = new ForecastQuery([]);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Explanation.Should().Be("Forecast unavailable.");
        _completionService.Verify(
            c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
