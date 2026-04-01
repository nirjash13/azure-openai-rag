namespace AzureAI.Application.DTOs;

/// <summary>Token consumption and estimated cost for a model invocation.</summary>
public sealed record TokenUsageDto(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCost);
