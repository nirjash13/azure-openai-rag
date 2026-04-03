namespace AzureAI.FunctionCalling.Abstractions;

public sealed record ToolCallRecord(string ToolName, string Arguments, string Result);
