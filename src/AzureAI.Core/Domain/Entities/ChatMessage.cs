using AzureAI.Core.Domain.Enums;

namespace AzureAI.Core.Domain.Entities;

/// <summary>A single message within a <see cref="ConversationSession"/>.</summary>
public sealed record ChatMessage
{
    /// <summary>Unique message identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifier of the owning conversation.</summary>
    public Guid ConversationId { get; init; }

    /// <summary>Role of the message author.</summary>
    public ChatRole Role { get; init; }

    /// <summary>Textual content of the message.</summary>
    public string Content { get; init; }

    /// <summary>UTC timestamp when the message was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Initializes a new <see cref="ChatMessage"/>.</summary>
    public ChatMessage(Guid conversationId, ChatRole role, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id             = Guid.NewGuid();
        ConversationId = conversationId;
        Role           = role;
        Content        = content;
        CreatedAt      = DateTime.UtcNow;
    }
}
