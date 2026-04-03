namespace AzureAI.FunctionCalling.Abstractions;

public interface IToolDefinition
{
    string Name { get; }
    string Description { get; }
    string ParameterSchema { get; }
    Task<string> ExecuteAsync(string argumentsJson, ToolExecutionContext context, CancellationToken cancellationToken);
}
