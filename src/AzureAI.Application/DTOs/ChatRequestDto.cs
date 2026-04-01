namespace AzureAI.Application.DTOs;

/// <summary>Incoming request payload for the ask-question endpoint.</summary>
public sealed record ChatRequestDto(
    string Question,
    Guid? ConversationId,
    ChatOptionsDto? Options);
