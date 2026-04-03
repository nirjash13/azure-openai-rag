using System.Reflection;
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

    /// <summary>
    /// Scans <paramref name="assembly"/> for <see cref="IToolDefinition"/> implementations
    /// decorated with <see cref="ToolFunctionAttribute"/> and registers them via the supplied factory.
    /// </summary>
    public void RegisterFromAssembly(Assembly assembly, Func<Type, IToolDefinition?> factory)
    {
        var types = assembly.GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract
                && t.GetCustomAttribute<ToolFunctionAttribute>() is not null
                && typeof(IToolDefinition).IsAssignableFrom(t));

        foreach (var type in types)
        {
            var tool = factory(type);
            if (tool is not null)
                Register(tool);
        }
    }
}
