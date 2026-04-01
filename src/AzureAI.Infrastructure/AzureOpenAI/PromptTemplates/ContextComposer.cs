using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Infrastructure.AzureOpenAI.PromptTemplates;

/// <summary>Assembles retrieved document chunks into a single context window string.</summary>
public static class ContextComposer
{
    /// <summary>
    /// Builds a context window from ranked search results, truncating when the estimated
    /// token count would exceed <paramref name="maxContextTokens"/>.
    /// </summary>
    /// <param name="results">Ranked search results to include in the context.</param>
    /// <param name="maxContextTokens">Upper bound on context size in tokens.</param>
    public static string BuildContextWindow(IReadOnlyList<SearchResult> results, int maxContextTokens)
    {
        if (results.Count == 0)
            return string.Empty;

        var sb          = new System.Text.StringBuilder();
        var tokensSoFar = 0;

        foreach (var result in results)
        {
            var citation = result.Metadata.PageNumber.HasValue
                ? $"[Source: {result.DocumentName}, page {result.Metadata.PageNumber}]"
                : $"[Source: {result.DocumentName}]";

            var block       = $"{citation}\n{result.ContentSnippet}\n\n";
            var blockTokens = EstimateTokens(block);

            if (tokensSoFar + blockTokens > maxContextTokens)
                break;

            sb.Append(block);
            tokensSoFar += blockTokens;
        }

        return sb.ToString().TrimEnd();
    }

    // Rough approximation: ~4 characters per token, consistent with GPT tokenization.
    private static int EstimateTokens(string text) => text.Length / 4;
}
