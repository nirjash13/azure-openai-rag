using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.ValueObjects;

namespace AzureAI.Core.Interfaces.Services;

/// <summary>The text content and token usage returned by a completion call.</summary>
/// <param name="Content">Generated assistant text.</param>
/// <param name="TokenUsage">Token counts and estimated cost for this call.</param>
public sealed record CompletionResult(string Content, TokenUsage TokenUsage);

/// <summary>Generates chat completions from a sequence of role-tagged messages.</summary>
public interface ICompletionService
{
    /// <summary>Sends a message history to the model and returns the assistant's response.</summary>
    /// <param name="messages">Ordered list of (role, content) pairs representing the conversation.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<CompletionResult> GenerateAsync(
        IReadOnlyList<(ChatRole Role, string Content)> messages,
        CancellationToken cancellationToken = default);
}
