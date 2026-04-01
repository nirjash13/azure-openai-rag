using AzureAI.Core.Domain.ValueObjects;

namespace AzureAI.Core.Domain.Entities;

/// <summary>A single text chunk extracted from a <see cref="Document"/> together with its embedding.</summary>
public sealed class DocumentChunk
{
    /// <summary>Unique chunk identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Identifier of the parent document.</summary>
    public Guid DocumentId { get; private set; }

    /// <summary>Raw text content of this chunk.</summary>
    public string Content { get; private set; }

    /// <summary>Dense vector representation of <see cref="Content"/>.</summary>
    public EmbeddingVector Embedding { get; private set; }

    /// <summary>Positional metadata within the source document.</summary>
    public ChunkMetadata Metadata { get; private set; }

    /// <summary>Zero-based index of this chunk within the document.</summary>
    public int ChunkIndex { get; private set; }

    /// <summary>UTC timestamp when the chunk record was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Initializes a new <see cref="DocumentChunk"/>.</summary>
    public DocumentChunk(
        Guid documentId,
        string content,
        EmbeddingVector embedding,
        ChunkMetadata metadata,
        int chunkIndex)
    {
        ArgumentNullException.ThrowIfNull(embedding);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), "Chunk index must be non-negative.");

        Id         = Guid.NewGuid();
        DocumentId = documentId;
        Content    = content;
        Embedding  = embedding;
        Metadata   = metadata;
        ChunkIndex = chunkIndex;
        CreatedAt  = DateTime.UtcNow;
    }
}
