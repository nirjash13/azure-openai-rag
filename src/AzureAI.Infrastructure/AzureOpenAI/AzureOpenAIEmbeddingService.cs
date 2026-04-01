using Azure;
using Azure.AI.OpenAI;
using AzureAI.Core.Configuration;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace AzureAI.Infrastructure.AzureOpenAI;

/// <summary>Generates text embeddings using Azure OpenAI's embedding models.</summary>
public sealed class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<AzureOpenAIEmbeddingService> _logger;

    /// <summary>Initializes a new <see cref="AzureOpenAIEmbeddingService"/>.</summary>
    public AzureOpenAIEmbeddingService(
        IOptions<AzureOpenAISettings> settings,
        ILogger<AzureOpenAIEmbeddingService> logger)
    {
        var s = settings.Value;
        var azureClient = new AzureOpenAIClient(new Uri(s.Endpoint), new AzureKeyCredential(s.ApiKey));
        _client = azureClient.GetEmbeddingClient(s.EmbeddingDeployment);
        _logger  = logger;
    }

    /// <inheritdoc />
    public async Task<EmbeddingVector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        var floats = result.Value.ToFloats().ToArray();

        _logger.LogDebug("Generated embedding with {Dimensions} dimensions", floats.Length);
        return new EmbeddingVector(floats);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmbeddingVector>> GenerateBatchEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        var result = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);

        var vectors = result.Value
            .OrderBy(e => e.Index)
            .Select(e => new EmbeddingVector(e.ToFloats().ToArray()))
            .ToList();

        _logger.LogDebug("Generated {Count} embeddings in batch", vectors.Count);
        return vectors;
    }
}
