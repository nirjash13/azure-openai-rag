namespace AzureAI.Application.DTOs;

public sealed record AnomalyDto(
    string ItemDescription,
    decimal Amount,
    decimal ExpectedAmount,
    double DeviationSigma,
    string Category,
    string? Explanation = null);
