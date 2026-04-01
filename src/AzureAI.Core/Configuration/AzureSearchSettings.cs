namespace AzureAI.Core.Configuration;

/// <summary>Strongly-typed settings for Azure AI Search, bound from the <c>AzureSearch</c> config section.</summary>
public sealed class AzureSearchSettings
{
    /// <summary>Azure AI Search service endpoint URL.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>API key used to authenticate requests.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Name of the search index that stores document chunks.</summary>
    public string IndexName { get; set; } = string.Empty;

    /// <summary>Name of the semantic configuration applied to queries.</summary>
    public string SemanticConfigName { get; set; } = string.Empty;

    /// <summary>Default number of top results to retrieve per query.</summary>
    public int TopK { get; set; }

    /// <summary>Minimum relevance score; results below this value are discarded.</summary>
    public double MinScore { get; set; }
}
