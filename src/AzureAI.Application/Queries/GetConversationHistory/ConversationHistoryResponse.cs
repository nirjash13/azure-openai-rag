namespace AzureAI.Application.Queries.GetConversationHistory;

/// <summary>A single message within a conversation history response.</summary>
public sealed record ConversationMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt);

/// <summary>Full history response for a conversation session.</summary>
public sealed record ConversationHistoryResponse(
    Guid ConversationId,
    string? Title,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    IReadOnlyList<ConversationMessageDto> Messages);
