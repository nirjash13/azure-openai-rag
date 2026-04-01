namespace AzureAI.Core.Domain.Exceptions;

/// <summary>
/// Thrown when a document cannot be processed or an illegal status transition is attempted.
/// </summary>
public sealed class DocumentProcessingException : Exception
{
    /// <summary>Identifier of the document that caused the failure.</summary>
    public Guid DocumentId { get; }

    /// <inheritdoc cref="DocumentProcessingException"/>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="message">Describes the failure.</param>
    /// <param name="innerException">Optional underlying exception.</param>
    public DocumentProcessingException(Guid documentId, string? message = null, Exception? innerException = null)
        : base(message ?? $"An error occurred while processing document '{documentId}'.", innerException)
    {
        DocumentId = documentId;
    }
}
