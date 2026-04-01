namespace AzureAI.Application.DTOs;

/// <summary>A source document cited in an assistant response.</summary>
public sealed record SourceCitationDto(
    string DocumentName,
    int? PageNumber,
    string? Section,
    double RelevanceScore,
    string Excerpt);
