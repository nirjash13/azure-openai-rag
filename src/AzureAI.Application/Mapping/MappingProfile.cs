using AzureAI.Application.DTOs;
using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Application.Mapping;

/// <summary>Manual mapping helpers between domain entities and DTOs.</summary>
public static class MappingProfile
{
    /// <summary>Maps a <see cref="Document"/> to its DTO projection.</summary>
    public static DocumentDto ToDto(this Document document) =>
        new(
            document.Id,
            document.FileName,
            document.ContentType,
            document.FileSize,
            document.DocumentType.ToString(),
            document.Status.ToString(),
            document.ChunkCount,
            document.CreatedAt,
            document.CompletedAt);

    /// <summary>Maps a <see cref="DocumentChunk"/> to its DTO projection.</summary>
    public static ChunkDto ToDto(this DocumentChunk chunk) =>
        new(
            chunk.Id,
            chunk.Content,
            chunk.ChunkIndex,
            chunk.Metadata.PageNumber,
            chunk.Metadata.Section);

    /// <summary>Maps a <see cref="TokenUsage"/> value object to its DTO.</summary>
    public static TokenUsageDto ToDto(this TokenUsage usage) =>
        new(usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens, usage.EstimatedCost);

    /// <summary>Maps a <see cref="SearchResult"/> to its DTO projection.</summary>
    public static SearchResultDto ToDto(this SearchResult result) =>
        new(
            result.DocumentName,
            result.ContentSnippet,
            result.Score,
            result.Metadata.PageNumber,
            result.Metadata.Section);

    /// <summary>Maps a <see cref="SearchResult"/> to a source citation DTO.</summary>
    public static SourceCitationDto ToCitationDto(this SearchResult result) =>
        new(
            result.DocumentName,
            result.Metadata.PageNumber,
            result.Metadata.Section,
            result.Score,
            result.ContentSnippet);
}
