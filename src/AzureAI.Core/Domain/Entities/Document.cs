using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.Exceptions;

namespace AzureAI.Core.Domain.Entities;

/// <summary>Represents an uploaded document tracked through the ingestion pipeline.</summary>
public sealed class Document
{
    /// <summary>Unique document identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Original file name as supplied by the caller.</summary>
    public string FileName { get; private set; }

    /// <summary>MIME content type of the file.</summary>
    public string ContentType { get; private set; }

    /// <summary>File size in bytes.</summary>
    public long FileSize { get; private set; }

    /// <summary>Parsed document format.</summary>
    public DocumentType DocumentType { get; private set; }

    /// <summary>Current ingestion lifecycle status.</summary>
    public IngestionStatus Status { get; private set; }

    /// <summary>Number of chunks produced from this document; 0 until ingestion completes.</summary>
    public int ChunkCount { get; private set; }

    /// <summary>Failure description when <see cref="Status"/> is <see cref="IngestionStatus.Failed"/>.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>UTC timestamp when the document record was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp when ingestion completed (successfully or not).</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="Document"/> in the <see cref="IngestionStatus.Pending"/> state.
    /// </summary>
    public Document(string fileName, string contentType, long fileSize, DocumentType documentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (fileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(fileSize), "File size must be non-negative.");

        Id           = Guid.NewGuid();
        FileName     = fileName;
        ContentType  = contentType;
        FileSize     = fileSize;
        DocumentType = documentType;
        Status       = IngestionStatus.Pending;
        CreatedAt    = DateTime.UtcNow;
    }

    /// <summary>Transitions the document to <see cref="IngestionStatus.Processing"/>.</summary>
    /// <exception cref="DocumentProcessingException">
    /// Thrown when the current status does not allow this transition.
    /// </exception>
    public void MarkProcessing()
    {
        if (Status != IngestionStatus.Pending)
            throw new DocumentProcessingException(Id,
                $"Cannot begin processing a document with status '{Status}'.");

        Status = IngestionStatus.Processing;
    }

    /// <summary>Transitions the document to <see cref="IngestionStatus.Completed"/>.</summary>
    /// <param name="chunkCount">Number of chunks produced.</param>
    /// <exception cref="DocumentProcessingException">
    /// Thrown when the current status does not allow this transition.
    /// </exception>
    public void MarkCompleted(int chunkCount)
    {
        if (Status != IngestionStatus.Processing)
            throw new DocumentProcessingException(Id,
                $"Cannot complete a document with status '{Status}'.");
        if (chunkCount < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkCount), "Chunk count must be non-negative.");

        ChunkCount  = chunkCount;
        Status      = IngestionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Transitions the document to <see cref="IngestionStatus.Failed"/>.</summary>
    /// <param name="reason">Human-readable description of the failure.</param>
    /// <exception cref="DocumentProcessingException">
    /// Thrown when the current status does not allow this transition.
    /// </exception>
    public void MarkFailed(string reason)
    {
        if (Status == IngestionStatus.Completed)
            throw new DocumentProcessingException(Id,
                "Cannot mark a completed document as failed.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        FailureReason = reason;
        Status        = IngestionStatus.Failed;
        CompletedAt   = DateTime.UtcNow;
    }
}
