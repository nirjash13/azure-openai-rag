namespace AzureAI.FunctionCalling.Abstractions;

public sealed record AgentResponse(
    string Content,
    IReadOnlyList<ToolCallRecord> ToolCallsExecuted,
    int IterationsUsed);
