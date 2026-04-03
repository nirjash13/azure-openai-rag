namespace AzureAI.FunctionCalling.Abstractions;

public sealed record AgentRequest(
    string Message,
    string? UserId = null,
    IReadOnlySet<string>? AllowedTools = null,
    Guid? ConversationId = null);
