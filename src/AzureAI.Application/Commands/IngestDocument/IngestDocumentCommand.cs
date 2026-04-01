using MediatR;

namespace AzureAI.Application.Commands.IngestDocument;

/// <summary>Command that triggers the full document ingestion pipeline for an uploaded file.</summary>
public sealed record IngestDocumentCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Guid>;
