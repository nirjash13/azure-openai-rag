namespace AzureAI.Core.Configuration;

/// <summary>Strongly-typed settings for RAG pipeline behaviour, bound from the <c>Rag</c> config section.</summary>
public sealed class RagSettings
{
    /// <summary>Maximum number of tokens that may be injected as retrieved context.</summary>
    public int MaxContextTokens { get; set; }

    /// <summary>Maximum number of conversation turns to include in the prompt.</summary>
    public int MaxConversationHistory { get; set; }

    /// <summary>When <c>true</c>, source citations are appended to assistant responses.</summary>
    public bool IncludeCitations { get; set; }
}
