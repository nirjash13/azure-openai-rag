using System.Text.Json;
using AzureAI.Application.DTOs;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureAI.Application.Queries.Anomalies;

public sealed class AnomalyDetectionHandler : IRequestHandler<AnomalyDetectionQuery, IReadOnlyList<AnomalyDto>>
{
    private const string SystemPrompt =
        "You are a financial analyst. For each anomaly provided, give a brief one-sentence explanation of why it might be unusual. " +
        "Return ONLY a JSON array with fields: item_description, explanation.";

    private readonly ICompletionService _completionService;
    private readonly ILogger<AnomalyDetectionHandler> _logger;

    public AnomalyDetectionHandler(ICompletionService completionService, ILogger<AnomalyDetectionHandler> logger)
    {
        _completionService = completionService;
        _logger            = logger;
    }

    public async Task<IReadOnlyList<AnomalyDto>> Handle(AnomalyDetectionQuery request, CancellationToken cancellationToken)
    {
        var statistical = DetectStatisticalAnomalies(request.Transactions, request.SigmaThreshold);

        if (statistical.Count == 0)
            return [];

        var explanations = await FetchExplanationsAsync(statistical, cancellationToken);

        return statistical.Select(a =>
        {
            var explanation = explanations.TryGetValue(a.ItemDescription, out var exp) ? exp : null;
            return a with { Explanation = explanation };
        }).ToList();
    }

    private static IReadOnlyList<AnomalyDto> DetectStatisticalAnomalies(
        IReadOnlyList<TransactionRecord> transactions,
        double sigmaThreshold)
    {
        var byCategory = transactions.GroupBy(t => t.Category);
        var anomalies  = new List<AnomalyDto>();

        foreach (var group in byCategory)
        {
            var items = group.ToList();
            if (items.Count < 2)
                continue;

            var amounts = items.Select(t => (double)t.Amount).ToArray();
            var mean    = amounts.Average();
            var stddev  = Math.Sqrt(amounts.Select(a => Math.Pow(a - mean, 2)).Average());

            if (stddev == 0)
                continue;

            foreach (var item in items)
            {
                var sigma = Math.Abs((double)item.Amount - mean) / stddev;
                if (sigma > sigmaThreshold)
                {
                    anomalies.Add(new AnomalyDto(
                        ItemDescription: item.Description,
                        Amount:          item.Amount,
                        ExpectedAmount:  (decimal)mean,
                        DeviationSigma:  sigma,
                        Category:        item.Category));
                }
            }
        }

        return anomalies;
    }

    private async Task<Dictionary<string, string>> FetchExplanationsAsync(
        IReadOnlyList<AnomalyDto> anomalies,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(anomalies.Select(a => new
            {
                item_description = a.ItemDescription,
                amount           = a.Amount,
                expected_amount  = a.ExpectedAmount,
                deviation_sigma  = a.DeviationSigma,
                category         = a.Category
            }));

            var messages = new List<(ChatRole Role, string Content)>
            {
                (ChatRole.System, SystemPrompt),
                (ChatRole.User, payload)
            };

            var result = await _completionService.GenerateAsync(messages, cancellationToken);
            return ParseExplanations(result.Content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch anomaly explanations");
            return [];
        }
    }

    private Dictionary<string, string> ParseExplanations(string content)
    {
        try
        {
            var json  = ExtractJson(content);
            var items = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (items is null)
                return [];

            return items.ToDictionary(
                e => e.GetProperty("item_description").GetString() ?? string.Empty,
                e => e.TryGetProperty("explanation", out var ex) ? ex.GetString() ?? string.Empty : string.Empty
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse explanation response");
            return [];
        }
    }

    private static string ExtractJson(string content)
    {
        var start = content.IndexOf('[');
        var end   = content.LastIndexOf(']');
        return start >= 0 && end > start ? content[start..(end + 1)] : content;
    }
}
