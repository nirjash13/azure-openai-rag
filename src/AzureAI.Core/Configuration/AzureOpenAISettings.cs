namespace AzureAI.Core.Configuration;

/// <summary>Strongly-typed settings for the Azure OpenAI service, bound from the <c>AzureOpenAI</c> config section.</summary>
public sealed class AzureOpenAISettings
{
    /// <summary>Azure OpenAI resource endpoint URL.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>API key used to authenticate requests.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Deployment name for the chat completion model.</summary>
    public string ChatDeployment { get; set; } = string.Empty;

    /// <summary>Deployment name for the text embedding model.</summary>
    public string EmbeddingDeployment { get; set; } = string.Empty;

    /// <summary>Maximum number of tokens to generate in a completion response.</summary>
    public int MaxTokens { get; set; }

    /// <summary>Sampling temperature in [0, 2]; higher values increase randomness.</summary>
    public double Temperature { get; set; }
}
