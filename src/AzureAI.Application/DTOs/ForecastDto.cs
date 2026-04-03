namespace AzureAI.Application.DTOs;

public sealed record ForecastDto(
    string Period,
    decimal ProjectedAmount,
    decimal LowerBound,
    decimal UpperBound,
    double ConfidenceLevel,
    string? Explanation = null);
