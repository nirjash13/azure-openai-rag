using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using AzureAI.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAI.Infrastructure.Search;

/// <summary>Creates and manages the Azure AI Search index schema.</summary>
public sealed class SearchIndexManager
{
    private readonly SearchIndexClient _indexClient;
    private readonly AzureSearchSettings _settings;
    private readonly ILogger<SearchIndexManager> _logger;

    /// <summary>Initializes a new <see cref="SearchIndexManager"/>.</summary>
    public SearchIndexManager(
        IOptions<AzureSearchSettings> settings,
        ILogger<SearchIndexManager> logger)
    {
        _settings    = settings.Value;
        _indexClient = new SearchIndexClient(
            new Uri(_settings.Endpoint),
            new AzureKeyCredential(_settings.ApiKey));
        _logger = logger;
    }

    /// <summary>
    /// Creates the search index if it does not already exist.
    /// The index includes a vector field with the specified <paramref name="vectorDimension"/>.
    /// </summary>
    /// <param name="vectorDimension">Dimensionality of the embedding vectors to store.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    public async Task EnsureIndexExistsAsync(int vectorDimension, CancellationToken cancellationToken = default)
    {
        var algorithmName = "hnsw-config";
        var profileName   = "vector-profile";

        var index = new SearchIndex(_settings.IndexName)
        {
            Fields =
            {
                new SimpleField("id",           SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SimpleField("documentId",   SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("documentName"),
                new SearchableField("content"),
                new SearchField("contentVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable      = true,
                    VectorSearchDimensions = vectorDimension,
                    VectorSearchProfileName = profileName,
                },
                new SimpleField("pageNumber",   SearchFieldDataType.Int32)  { IsFilterable = true },
                new SimpleField("section",      SearchFieldDataType.String) { IsFilterable = true },
            },
            VectorSearch = new VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile(profileName, algorithmName),
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(algorithmName),
                },
            },
        };

        await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
        _logger.LogInformation("Search index '{IndexName}' is ready", _settings.IndexName);
    }
}
