namespace AzureAI.Application.DTOs;

/// <summary>Optional tuning parameters for a chat completion request.</summary>
public sealed record ChatOptionsDto(
    int? TopK,
    bool? IncludeCitations);
