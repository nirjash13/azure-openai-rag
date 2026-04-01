using AzureAI.Core.Domain.ValueObjects;

namespace AzureAI.Core.Interfaces.Services;

/// <summary>Generates dense vector embeddings for text using an underlying language model.</summary>
public interface IEmbeddingService
{
    /// <summary>Generates an embedding vector for a single piece of text.</summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<EmbeddingVector> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Generates embedding vectors for a batch of texts in a single call.</summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<IReadOnlyList<EmbeddingVector>> GenerateBatchEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
