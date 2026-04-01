namespace AzureAI.Application.DTOs;

/// <summary>Response payload returned by the ask-question endpoint.</summary>
public sealed record ChatResponseDto(
    string Answer,
    List<SourceCitationDto> Sources,
    TokenUsageDto TokenUsage,
    long ProcessingTimeMs,
    Guid? ConversationId);
