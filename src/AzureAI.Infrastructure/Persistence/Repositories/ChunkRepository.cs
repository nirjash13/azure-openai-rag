using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AzureAI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IChunkRepository"/>.</summary>
public sealed class ChunkRepository : IChunkRepository
{
    private readonly AzureAIDbContext _db;

    /// <summary>Initializes a new <see cref="ChunkRepository"/>.</summary>
    public ChunkRepository(AzureAIDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task AddRangeAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _db.DocumentChunks.AddRangeAsync(chunks, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _db.DocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await _db.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
