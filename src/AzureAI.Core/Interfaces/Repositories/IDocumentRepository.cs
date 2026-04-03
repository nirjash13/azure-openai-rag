using AzureAI.Core.Domain.Entities;

namespace AzureAI.Core.Interfaces.Repositories;

/// <summary>Persistence contract for <see cref="Document"/> aggregates.</summary>
public interface IDocumentRepository
{
    /// <summary>Persists a new document.</summary>
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>Returns the document with the given identifier (no-tracking), or <c>null</c> if not found.</summary>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the document with the given identifier as a tracked entity for update, or <c>null</c> if not found.</summary>
    Task<Document?> GetByIdTrackingAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all documents in the store.</summary>
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to an existing document.</summary>
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);

    /// <summary>Removes the document with the given identifier.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
