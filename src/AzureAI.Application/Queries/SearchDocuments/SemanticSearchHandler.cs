using AzureAI.Application.DTOs;
using AzureAI.Application.Mapping;
using AzureAI.Core.Configuration;
using AzureAI.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace AzureAI.Application.Queries.SearchDocuments;

/// <summary>Embeds the query and returns ranked document chunks from the search index.</summary>
public sealed class SemanticSearchHandler : IRequestHandler<SemanticSearchQuery, IReadOnlyList<SearchResultDto>>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly AzureSearchSettings _searchSettings;

    /// <summary>Initializes a new <see cref="SemanticSearchHandler"/>.</summary>
    public SemanticSearchHandler(
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearchService,
        IOptions<AzureSearchSettings> searchSettings)
    {
        _embeddingService    = embeddingService;
        _vectorSearchService = vectorSearchService;
        _searchSettings      = searchSettings.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResultDto>> Handle(
        SemanticSearchQuery request,
        CancellationToken cancellationToken)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
        var results   = await _vectorSearchService.SearchAsync(
            embedding,
            request.TopK    ?? _searchSettings.TopK,
            request.MinScore ?? _searchSettings.MinScore,
            cancellationToken);

        return results.Select(r => r.ToDto()).ToList();
    }
}
