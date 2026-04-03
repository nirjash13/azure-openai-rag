using AzureAI.Core.Domain.Enums;

namespace AzureAI.Core.Domain.Entities;

/// <summary>A multi-turn conversation between the user and the assistant.</summary>
public sealed class ConversationSession
{
    private readonly List<ChatMessage> _messages = [];

    /// <summary>Unique session identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Optional display title for the session.</summary>
    public string? Title { get; private set; }

    /// <summary>Ordered list of messages in this conversation.</summary>
    public IReadOnlyList<ChatMessage> Messages => _messages;

    /// <summary>UTC timestamp when the session was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp of the most recent message, or the session creation time if no messages exist.</summary>
    public DateTime LastMessageAt { get; private set; }

    // Required by EF Core for materialization.
    private ConversationSession()
    {
    }

    /// <summary>Initializes a new <see cref="ConversationSession"/>.</summary>
    /// <param name="title">Optional title; can be set or updated later.</param>
    public ConversationSession(string? title = null)
    {
        Id            = Guid.NewGuid();
        Title         = title;
        CreatedAt     = DateTime.UtcNow;
        LastMessageAt = CreatedAt;
    }

    /// <summary>Appends a new message to this session.</summary>
    /// <param name="role">Role of the message author.</param>
    /// <param name="content">Text content of the message.</param>
    /// <returns>The newly created <see cref="ChatMessage"/>.</returns>
    public ChatMessage AddMessage(ChatRole role, string content)
    {
        var message = new ChatMessage(Id, role, content);
        _messages.Add(message);
        LastMessageAt = message.CreatedAt;
        return message;
    }

    /// <summary>Updates the display title of this session.</summary>
    public void SetTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }
}
