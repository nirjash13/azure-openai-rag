using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.ValueObjects;

namespace AzureAI.Core.Interfaces.Services;

/// <summary>A ranked result returned by a vector similarity search.</summary>
/// <param name="ChunkId">Identifier of the matching chunk.</param>
/// <param name="DocumentId">Identifier of the document that contains the chunk.</param>
/// <param name="DocumentName">Display name of the source document.</param>
/// <param name="ContentSnippet">Excerpt of the chunk content.</param>
/// <param name="Score">Relevance score as returned by the search backend.</param>
/// <param name="Metadata">Positional metadata for the chunk within its source document.</param>
public sealed record SearchResult(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentName,
    string ContentSnippet,
    double Score,
    ChunkMetadata Metadata);

/// <summary>Performs approximate nearest-neighbour search over indexed document chunks.</summary>
public interface IVectorSearchService
{
    /// <summary>Retrieves the top-<paramref name="topK"/> chunks most similar to <paramref name="queryEmbedding"/>.</summary>
    /// <param name="queryEmbedding">Query vector.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="minimumScore">Lower bound on the relevance score; results below this threshold are excluded.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        EmbeddingVector queryEmbedding,
        int topK,
        double minimumScore,
        CancellationToken cancellationToken = default);

    /// <summary>Upserts all supplied chunks into the search index.</summary>
    /// <param name="chunks">Chunks to index.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task IndexChunksAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default);

    /// <summary>Removes all indexed chunks belonging to the specified document.</summary>
    /// <param name="documentId">Identifier of the document whose chunks should be removed.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task RemoveDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
}
