using AzureAI.Core.Domain.Entities;

namespace AzureAI.Core.Interfaces.Repositories;

/// <summary>Persistence contract for <see cref="ConversationSession"/> aggregates.</summary>
public interface IConversationRepository
{
    /// <summary>Persists a new conversation session.</summary>
    Task CreateAsync(ConversationSession session, CancellationToken cancellationToken = default);

    /// <summary>Returns the session with the given identifier, or <c>null</c> if not found.</summary>
    Task<ConversationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent messages from a conversation, ordered oldest-first.</summary>
    /// <param name="conversationId">Target conversation.</param>
    /// <param name="maxMessages">Maximum number of messages to return.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(
        Guid conversationId,
        int maxMessages,
        CancellationToken cancellationToken = default);

    /// <summary>Appends one or more messages to an existing conversation and updates <c>LastMessageAt</c>.</summary>
    Task AppendMessagesAsync(Guid conversationId, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>Removes the conversation and all its messages.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
