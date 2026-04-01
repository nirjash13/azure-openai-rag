namespace AzureAI.Core.Domain.ValueObjects;

/// <summary>Token consumption and estimated cost for a single model invocation.</summary>
/// <param name="PromptTokens">Number of tokens in the prompt.</param>
/// <param name="CompletionTokens">Number of tokens in the completion.</param>
/// <param name="TotalTokens">Sum of prompt and completion tokens.</param>
/// <param name="EstimatedCost">Approximate cost in USD for this invocation.</param>
public sealed record TokenUsage(
    int PromptTokens,
    int CompletionTokens,
    int TotalTokens,
    decimal EstimatedCost)
{
    /// <summary>
    /// Creates a <see cref="TokenUsage"/> instance with the estimated cost calculated using
    /// the supplied per-token rates.
    /// </summary>
    /// <param name="promptTokens">Prompt token count.</param>
    /// <param name="completionTokens">Completion token count.</param>
    /// <param name="promptCostPerToken">Cost per prompt token in USD.</param>
    /// <param name="completionCostPerToken">Cost per completion token in USD.</param>
    public static TokenUsage Calculate(
        int promptTokens,
        int completionTokens,
        decimal promptCostPerToken,
        decimal completionCostPerToken)
    {
        var cost = promptTokens * promptCostPerToken + completionTokens * completionCostPerToken;
        return new TokenUsage(promptTokens, completionTokens, promptTokens + completionTokens, cost);
    }
}
