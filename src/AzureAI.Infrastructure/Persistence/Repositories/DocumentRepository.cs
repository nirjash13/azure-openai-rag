using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AzureAI.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IDocumentRepository"/>.</summary>
public sealed class DocumentRepository : IDocumentRepository
{
    private readonly AzureAIDbContext _db;

    /// <summary>Initializes a new <see cref="DocumentRepository"/>.</summary>
    public DocumentRepository(AzureAIDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _db.Documents.AddAsync(document, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Document?> GetByIdTrackingAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Documents.AsNoTracking().OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        _db.Documents.Update(document);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _db.Documents.Where(d => d.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}
