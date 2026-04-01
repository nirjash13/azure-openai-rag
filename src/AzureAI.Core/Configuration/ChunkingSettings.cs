namespace AzureAI.Core.Configuration;

/// <summary>Strongly-typed settings that control document chunking, bound from the <c>Chunking</c> config section.</summary>
public sealed class ChunkingSettings
{
    /// <summary>Target token count for each chunk.</summary>
    public int ChunkSizeTokens { get; set; }

    /// <summary>Number of tokens shared between consecutive chunks.</summary>
    public int OverlapTokens { get; set; }

    /// <summary>Upper bound on the number of chunks produced from a single document.</summary>
    public int MaxChunksPerDocument { get; set; }
}
