using AzureAI.FunctionCalling.Abstractions;

namespace AzureAI.FunctionCalling;

public sealed class ToolRegistry
{
    private readonly Dictionary<string, IToolDefinition> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IToolDefinition tool) => _tools[tool.Name] = tool;

    public IToolDefinition? GetTool(string name) => _tools.TryGetValue(name, out var t) ? t : null;

    public IReadOnlyList<IToolDefinition> GetAllTools() => [.. _tools.Values];

    public IReadOnlyList<IToolDefinition> GetAllowedTools(IReadOnlySet<string>? allowedTools)
    {
        if (allowedTools is null) return GetAllTools();
        return [.. _tools.Values.Where(t => allowedTools.Contains(t.Name))];
    }
}
