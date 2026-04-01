using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AzureAI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IConversationRepository"/>.</summary>
public sealed class ConversationRepository : IConversationRepository
{
    private readonly AzureAIDbContext _db;

    /// <summary>Initializes a new <see cref="ConversationRepository"/>.</summary>
    public ConversationRepository(AzureAIDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task CreateAsync(ConversationSession session, CancellationToken cancellationToken = default)
    {
        await _db.Conversations.AddAsync(session, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<ConversationSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Conversations
            .Include(c => c.Messages)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(
        Guid conversationId,
        int maxMessages,
        CancellationToken cancellationToken = default)
    {
        return await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(maxMessages)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AppendMessagesAsync(Guid conversationId, params ChatMessage[] messages)
    {
        await _db.ChatMessages.AddRangeAsync(messages);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Conversations.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}
