using System.Text.Json;
using AzureAI.Application.DTOs;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureAI.Application.Queries.Recommendations;

public sealed class BudgetRecommendationHandler : IRequestHandler<BudgetRecommendationQuery, IReadOnlyList<RecommendationDto>>
{
    private const string SystemPrompt =
        "You are a budget optimization expert. Analyze spending patterns and suggest actionable budget recommendations. " +
        "Return ONLY a JSON array with fields: category, recommendation, potential_savings, priority, rationale. " +
        "Priority must be one of: High, Medium, Low. Potential savings must be a number.";

    private readonly ICompletionService _completionService;
    private readonly ILogger<BudgetRecommendationHandler> _logger;

    public BudgetRecommendationHandler(ICompletionService completionService, ILogger<BudgetRecommendationHandler> logger)
    {
        _completionService = completionService;
        _logger            = logger;
    }

    public async Task<IReadOnlyList<RecommendationDto>> Handle(BudgetRecommendationQuery request, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            total_budget     = request.TotalBudget,
            current_spending = request.CurrentSpending.Select(s => new
            {
                category = s.Category,
                amount   = s.Amount,
                budget   = s.Budget
            })
        });

        var messages = new List<(ChatRole Role, string Content)>
        {
            (ChatRole.System, SystemPrompt),
            (ChatRole.User, payload)
        };

        var result = await _completionService.GenerateAsync(messages, cancellationToken);

        return ParseRecommendations(result.Content);
    }

    private IReadOnlyList<RecommendationDto> ParseRecommendations(string content)
    {
        try
        {
            var json  = ExtractJson(content);
            var items = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (items is null || items.Length == 0)
                return [];

            return items.Select(e => new RecommendationDto(
                Category:         e.GetProperty("category").GetString() ?? string.Empty,
                Recommendation:   e.GetProperty("recommendation").GetString() ?? string.Empty,
                PotentialSavings: e.GetProperty("potential_savings").GetDecimal(),
                Priority:         e.GetProperty("priority").GetString() ?? string.Empty,
                Rationale:        e.TryGetProperty("rationale", out var r) ? r.GetString() : null
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse recommendation response");
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
