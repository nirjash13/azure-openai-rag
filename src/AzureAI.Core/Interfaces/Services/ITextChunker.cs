namespace AzureAI.Core.Interfaces.Services;

/// <summary>A contiguous segment of text produced by <see cref="ITextChunker"/>.</summary>
/// <param name="Content">Text content of the chunk.</param>
/// <param name="ChunkIndex">Zero-based position of this chunk within the source text.</param>
/// <param name="StartIndex">Character offset of the first character in the source text.</param>
/// <param name="EndIndex">Character offset immediately after the last character in the source text.</param>
/// <param name="PageNumber">Optional page number from which this chunk originates.</param>
/// <param name="Section">Optional section heading under which this chunk falls.</param>
public sealed record TextChunk(
    string Content,
    int ChunkIndex,
    int StartIndex,
    int EndIndex,
    int? PageNumber = null,
    string? Section = null);

/// <summary>Splits document text into overlapping chunks suitable for embedding.</summary>
public interface ITextChunker
{
    /// <summary>Partitions <paramref name="text"/> into chunks of at most <paramref name="chunkSizeTokens"/> tokens,
    /// with a sliding overlap of <paramref name="overlapTokens"/> tokens between adjacent chunks.</summary>
    /// <param name="text">Source text to chunk.</param>
    /// <param name="chunkSizeTokens">Maximum token count per chunk.</param>
    /// <param name="overlapTokens">Number of tokens shared between consecutive chunks.</param>
    IReadOnlyList<TextChunk> ChunkText(string text, int chunkSizeTokens, int overlapTokens);
}
