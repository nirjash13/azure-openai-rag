using AzureAI.Core.Domain.Entities;

namespace AzureAI.Core.Interfaces.Repositories;

/// <summary>Persistence contract for <see cref="DocumentChunk"/> records.</summary>
public interface IChunkRepository
{
    /// <summary>Persists a batch of chunks in a single operation.</summary>
    Task AddRangeAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    /// <summary>Returns all chunks belonging to the specified document.</summary>
    Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Removes all chunks belonging to the specified document.</summary>
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}
