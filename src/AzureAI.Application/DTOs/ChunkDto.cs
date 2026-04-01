namespace AzureAI.Application.DTOs;

/// <summary>Projection of a <see cref="AzureAI.Core.Domain.Entities.DocumentChunk"/> for API responses.</summary>
public sealed record ChunkDto(
    Guid Id,
    string Content,
    int ChunkIndex,
    int? PageNumber,
    string? Section);
