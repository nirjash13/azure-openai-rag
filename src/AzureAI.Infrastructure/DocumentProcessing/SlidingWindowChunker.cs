using System.Text.RegularExpressions;
using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>
/// Splits document text into overlapping chunks that respect sentence boundaries.
/// Uses a sliding window so adjacent chunks share a configurable number of tokens,
/// preserving context across chunk boundaries.
/// </summary>
public sealed class SlidingWindowChunker : ITextChunker
{
    // Matches sentence-ending punctuation followed by whitespace and an uppercase letter or end of string.
    private static readonly Regex SentenceSplitter = new(
        @"(?<=[.!?])\s+(?=[A-Z\u0980-\u09FF])|(?<=[.!?])\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <inheritdoc />
    public IReadOnlyList<TextChunk> ChunkText(string text, int chunkSizeTokens, int overlapTokens)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var sentences = SplitSentences(text);
        if (sentences.Count == 0)
            return [];

        var chunks              = new List<TextChunk>();
        var currentSentences    = new List<string>();
        var currentTokens       = 0;
        var chunkIndex          = 0;
        var charOffset          = 0;
        var chunkStartOffset    = 0;

        foreach (var sentence in sentences)
        {
            var sentenceTokens = EstimateTokens(sentence);

            if (currentTokens + sentenceTokens > chunkSizeTokens && currentSentences.Count > 0)
            {
                var content    = string.Join(" ", currentSentences);
                var endOffset  = charOffset;
                chunks.Add(new TextChunk(content, chunkIndex++, chunkStartOffset, endOffset));

                // Retain overlap from the tail of the current chunk.
                var (overlapSentences, overlapStart) = BuildOverlap(currentSentences, overlapTokens, chunkStartOffset);
                currentSentences  = overlapSentences;
                currentTokens     = currentSentences.Sum(EstimateTokens);
                chunkStartOffset  = overlapStart;
            }

            currentSentences.Add(sentence);
            currentTokens += sentenceTokens;
            charOffset    += sentence.Length + 1; // +1 for the space between sentences
        }

        if (currentSentences.Count > 0)
        {
            var content = string.Join(" ", currentSentences);
            chunks.Add(new TextChunk(content, chunkIndex, chunkStartOffset, charOffset));
        }

        return chunks;
    }

    private static List<string> SplitSentences(string text)
    {
        var parts = SentenceSplitter.Split(text);
        return parts
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static (List<string> sentences, int startOffset) BuildOverlap(
        List<string> sentences,
        int overlapTokens,
        int baseOffset)
    {
        var overlap      = new List<string>();
        var tokenCount   = 0;
        var charsFromEnd = 0;

        for (var i = sentences.Count - 1; i >= 0 && tokenCount < overlapTokens; i--)
        {
            overlap.Insert(0, sentences[i]);
            tokenCount   += EstimateTokens(sentences[i]);
            charsFromEnd += sentences[i].Length + 1;
        }

        var totalLength  = sentences.Sum(s => s.Length + 1);
        var startOffset  = baseOffset + totalLength - charsFromEnd;
        return (overlap, Math.Max(baseOffset, startOffset));
    }

    private static readonly Lazy<Microsoft.ML.Tokenizers.Tokenizer> _tokenizer =
        new(() =>
        {
            try
            {
                return Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel("gpt-4o");
            }
            catch
            {
                return null!;
            }
        });

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        if (_tokenizer.Value is null) return Math.Max(1, text.Length / 4);
        return _tokenizer.Value.CountTokens(text);
    }
}
