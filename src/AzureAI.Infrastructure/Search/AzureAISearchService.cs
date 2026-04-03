using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using AzureAI.Core.Configuration;
using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAI.Infrastructure.Search;

/// <summary>Implements vector similarity search and indexing over Azure AI Search.</summary>
public sealed class AzureAISearchService : IVectorSearchService
{
    private readonly SearchClient _searchClient;
    private readonly AzureSearchSettings _settings;
    private readonly ILogger<AzureAISearchService> _logger;

    /// <summary>Initializes a new <see cref="AzureAISearchService"/>.</summary>
    public AzureAISearchService(
        IOptions<AzureSearchSettings> settings,
        ILogger<AzureAISearchService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings         = settings.Value;
        var httpClient    = httpClientFactory.CreateClient("AzureSearch");
        var searchOptions = new SearchClientOptions();
        searchOptions.Transport = new Azure.Core.Pipeline.HttpClientTransport(httpClient);
        _searchClient = new SearchClient(
            new Uri(_settings.Endpoint),
            _settings.IndexName,
            new AzureKeyCredential(_settings.ApiKey),
            searchOptions);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        EmbeddingVector queryEmbedding,
        int topK,
        double minimumScore,
        CancellationToken cancellationToken = default)
    {
        var vectorQuery = new VectorizedQuery(queryEmbedding.ToArray())
        {
            KNearestNeighborsCount = topK,
            Fields                 = { "contentVector" },
        };

        var options = new SearchOptions
        {
            VectorSearch = new() { Queries = { vectorQuery } },
            Size         = topK,
            Select       = { "id", "documentId", "documentName", "content", "pageNumber", "section", "startIndex", "endIndex" },
        };

        var response = await _searchClient.SearchAsync<SearchDocument>("*", options, cancellationToken);
        var results  = new List<SearchResult>();

        await foreach (var page in response.Value.GetResultsAsync().AsPages().WithCancellation(cancellationToken))
        {
            foreach (var r in page.Values)
            {
                if (r.Score < minimumScore)
                    continue;

                var doc = r.Document;
                results.Add(new SearchResult(
                    ChunkId:        ParseGuid(doc, "id"),
                    DocumentId:     ParseGuid(doc, "documentId"),
                    DocumentName:   ParseString(doc, "documentName"),
                    ContentSnippet: ParseString(doc, "content"),
                    Score:          r.Score ?? 0,
                    Metadata:       new ChunkMetadata(
                        StartIndex:  ParseInt(doc, "startIndex"),
                        EndIndex:    ParseInt(doc, "endIndex"),
                        PageNumber:  ParseNullableInt(doc, "pageNumber"),
                        Section:     ParseNullableString(doc, "section"))));
            }
        }

        _logger.LogDebug("Vector search returned {Count} results (min score: {MinScore})", results.Count, minimumScore);
        return results;
    }

    /// <inheritdoc />
    public async Task IndexChunksAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
            return;

        var documents = chunks.Select(c => new SearchDocument
        {
            ["id"]            = c.Id.ToString(),
            ["documentId"]    = c.DocumentId.ToString(),
            ["documentName"]  = c.DocumentName,
            ["content"]       = c.Content,
            ["contentVector"] = c.Embedding.ToArray(),
            ["pageNumber"]    = c.Metadata.PageNumber,
            ["section"]       = c.Metadata.Section,
            ["startIndex"]    = c.Metadata.StartIndex,
            ["endIndex"]      = c.Metadata.EndIndex,
        }).ToList();

        var batch = IndexDocumentsBatch.Upload(documents);
        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

        _logger.LogDebug("Indexed {Count} chunks in Azure AI Search", chunks.Count);
    }

    /// <inheritdoc />
    public async Task RemoveDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        const int pageSize = 1000;
        var filter = $"documentId eq '{documentId}'";
        var ids    = new List<string>();
        int skip   = 0;

        while (true)
        {
            var options  = new SearchOptions { Filter = filter, Select = { "id" }, Size = pageSize, Skip = skip };
            var response = await _searchClient.SearchAsync<SearchDocument>("*", options, cancellationToken);
            var page     = new List<string>();

            await foreach (var r in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
                page.Add(ParseString(r.Document, "id"));

            ids.AddRange(page);
            if (page.Count < pageSize)
                break;

            skip += pageSize;
        }

        if (ids.Count == 0)
            return;

        var deleteDocs = ids.Select(id => new SearchDocument { ["id"] = id }).ToList();
        var batch      = IndexDocumentsBatch.Delete(deleteDocs);
        await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

        _logger.LogDebug("Removed {Count} indexed chunks for document {DocumentId}", ids.Count, documentId);
    }

    private static Guid ParseGuid(SearchDocument doc, string key) =>
        doc.TryGetValue(key, out var v) && v is string s && Guid.TryParse(s, out var g) ? g : Guid.Empty;

    private static string ParseString(SearchDocument doc, string key) =>
        doc.TryGetValue(key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;

    private static string? ParseNullableString(SearchDocument doc, string key) =>
        doc.TryGetValue(key, out var v) ? v?.ToString() : null;

    private static int? ParseNullableInt(SearchDocument doc, string key) =>
        doc.TryGetValue(key, out var v) && v is int i ? i : null;

    private static int ParseInt(SearchDocument doc, string key) =>
        doc.TryGetValue(key, out var v) && v is int i ? i : 0;
}
