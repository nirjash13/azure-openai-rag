namespace AzureAI.Application.DTOs;

public sealed record RecommendationDto(
    string Category,
    string Recommendation,
    decimal PotentialSavings,
    string Priority,
    string? Rationale = null);
