namespace AzureAI.Core.Domain.Exceptions;

/// <summary>Thrown when the embedding service fails to produce a vector for the supplied text.</summary>
public sealed class EmbeddingGenerationException : Exception
{
    /// <inheritdoc cref="EmbeddingGenerationException"/>
    /// <param name="message">Describes the failure.</param>
    /// <param name="innerException">Optional underlying exception.</param>
    public EmbeddingGenerationException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
