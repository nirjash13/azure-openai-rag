namespace AzureAI.Core.Domain.ValueObjects;

/// <summary>Positional and structural metadata for a text chunk within its source document.</summary>
/// <param name="StartIndex">Character offset of the first character of the chunk.</param>
/// <param name="EndIndex">Character offset immediately after the last character of the chunk.</param>
/// <param name="PageNumber">Optional page number within the source document.</param>
/// <param name="Section">Optional section heading under which the chunk falls.</param>
public sealed record ChunkMetadata(
    int StartIndex,
    int EndIndex,
    int? PageNumber = null,
    string? Section = null);
