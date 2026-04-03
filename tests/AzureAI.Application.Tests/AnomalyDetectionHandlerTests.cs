using AzureAI.Application.Queries.Anomalies;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureAI.Application.Tests;

public sealed class AnomalyDetectionHandlerTests
{
    private readonly Mock<ICompletionService> _completionService = new();
    private readonly Mock<ILogger<AnomalyDetectionHandler>> _logger = new();

    private AnomalyDetectionHandler CreateHandler() =>
        new(_completionService.Object, _logger.Object);

    private void SetupCompletion(string content)
    {
        var usage = new TokenUsage(5, 10, 15, 0.005m);
        _completionService
            .Setup(c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(content, usage));
    }

    [Fact]
    public async Task Handle_OutlierTransaction_ReturnedAsAnomaly()
    {
        SetupCompletion("""[{"item_description":"Giant expense","explanation":"Unusually large purchase."}]""");

        var transactions = new List<TransactionRecord>
        {
            new("Normal 1",  100m, "Office", "2024-01-01"),
            new("Normal 2",  100m, "Office", "2024-01-02"),
            new("Normal 3",  100m, "Office", "2024-01-03"),
            new("Normal 4",  100m, "Office", "2024-01-04"),
            new("Normal 5",  100m, "Office", "2024-01-05"),
            new("Normal 6",  100m, "Office", "2024-01-06"),
            new("Normal 7",  100m, "Office", "2024-01-07"),
            new("Normal 8",  100m, "Office", "2024-01-08"),
            new("Normal 9",  100m, "Office", "2024-01-09"),
            new("Giant expense", 10000m, "Office", "2024-01-10"),
        };

        var handler = CreateHandler();
        var result  = await handler.Handle(new AnomalyDetectionQuery(transactions, SigmaThreshold: 2.0), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ItemDescription.Should().Be("Giant expense");
        result[0].DeviationSigma.Should().BeGreaterThan(2.0);
        result[0].Explanation.Should().Be("Unusually large purchase.");
    }

    [Fact]
    public async Task Handle_AllWithinThreshold_ReturnsEmpty()
    {
        var transactions = new List<TransactionRecord>
        {
            new("Item A", 100m, "Food", "2024-01-01"),
            new("Item B", 105m, "Food", "2024-01-02"),
            new("Item C",  95m, "Food", "2024-01-03"),
        };

        var handler = CreateHandler();
        var result  = await handler.Handle(new AnomalyDetectionQuery(transactions, SigmaThreshold: 2.0), CancellationToken.None);

        result.Should().BeEmpty();
        _completionService.Verify(
            c => c.GenerateAsync(It.IsAny<IReadOnlyList<(ChatRole, string)>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SingleItemPerCategory_CategorySkipped()
    {
        var transactions = new List<TransactionRecord>
        {
            new("Only item", 999m, "Unique", "2024-01-01"),
        };

        var handler = CreateHandler();
        var result  = await handler.Handle(new AnomalyDetectionQuery(transactions), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
