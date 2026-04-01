namespace AzureAI.Application.DTOs;

/// <summary>A single ranked result from a semantic search query.</summary>
public sealed record SearchResultDto(
    string DocumentName,
    string ContentSnippet,
    double Score,
    int? PageNumber,
    string? Section);
