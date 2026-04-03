using System.Text;
using System.Text.Json;
using AzureAI.Application.DTOs;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureAI.Application.Queries.Forecast;

public sealed class ForecastHandler : IRequestHandler<ForecastQuery, IReadOnlyList<ForecastDto>>
{
    private const string SystemPrompt =
        "You are a financial forecasting expert. Analyze the historical data and generate forecasts. " +
        "Return ONLY a JSON array of objects with fields: period, projected_amount, lower_bound, upper_bound, confidence_level, explanation. " +
        "Do not include any prose outside the JSON array.";

    private readonly ICompletionService _completionService;
    private readonly ILogger<ForecastHandler> _logger;

    public ForecastHandler(ICompletionService completionService, ILogger<ForecastHandler> logger)
    {
        _completionService = completionService;
        _logger            = logger;
    }

    public async Task<IReadOnlyList<ForecastDto>> Handle(ForecastQuery request, CancellationToken cancellationToken)
    {
        if (request.HistoricalData.Count == 0)
            return Fallback();

        var csv = BuildCsv(request.HistoricalData);
        var userMessage = $"Historical data (CSV):\n{csv}\n\nForecast the next {request.PeriodsToForecast} periods.";

        var messages = new List<(ChatRole Role, string Content)>
        {
            (ChatRole.System, SystemPrompt),
            (ChatRole.User, userMessage)
        };

        var result = await _completionService.GenerateAsync(messages, cancellationToken);

        return ParseForecasts(result.Content);
    }

    private static string BuildCsv(IReadOnlyList<HistoricalPeriod> data)
    {
        var sb = new StringBuilder("Period,Amount\n");
        foreach (var p in data)
            sb.AppendLine($"{p.Period},{p.Amount}");
        return sb.ToString();
    }

    private IReadOnlyList<ForecastDto> ParseForecasts(string content)
    {
        try
        {
            var json = ExtractJson(content);
            var items = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (items is null || items.Length == 0)
                return Fallback();

            return items.Select(e => new ForecastDto(
                Period:          e.GetProperty("period").GetString() ?? string.Empty,
                ProjectedAmount: e.GetProperty("projected_amount").GetDecimal(),
                LowerBound:      e.GetProperty("lower_bound").GetDecimal(),
                UpperBound:      e.GetProperty("upper_bound").GetDecimal(),
                ConfidenceLevel: e.GetProperty("confidence_level").GetDouble(),
                Explanation:     e.TryGetProperty("explanation", out var ex) ? ex.GetString() : null
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse forecast response");
            return Fallback();
        }
    }

    private static string ExtractJson(string content)
    {
        var start = content.IndexOf('[');
        var end   = content.LastIndexOf(']');
        return start >= 0 && end > start ? content[start..(end + 1)] : content;
    }

    private static IReadOnlyList<ForecastDto> Fallback() =>
        [new ForecastDto(string.Empty, 0, 0, 0, 0.0, "Forecast unavailable.")];
}
