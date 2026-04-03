using AzureAI.Application.DTOs;
using MediatR;

namespace AzureAI.Application.Queries.Recommendations;

public sealed record BudgetRecommendationQuery(
    IReadOnlyList<SpendingCategory> CurrentSpending,
    decimal? TotalBudget = null) : IRequest<IReadOnlyList<RecommendationDto>>;

public sealed record SpendingCategory(
    string Category,
    decimal Amount,
    decimal? Budget = null);
