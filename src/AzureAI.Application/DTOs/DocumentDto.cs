namespace AzureAI.Application.DTOs;

/// <summary>Projection of a <see cref="AzureAI.Core.Domain.Entities.Document"/> for API responses.</summary>
public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    string DocumentType,
    string Status,
    int ChunkCount,
    DateTime CreatedAt,
    DateTime? CompletedAt);
