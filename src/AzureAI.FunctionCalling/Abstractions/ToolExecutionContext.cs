namespace AzureAI.FunctionCalling.Abstractions;

public sealed record ToolExecutionContext(
    string? UserId,
    IReadOnlySet<string> AllowedTools,
    bool IsDestructiveConfirmed = false);
