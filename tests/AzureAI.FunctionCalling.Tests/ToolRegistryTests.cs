using AzureAI.FunctionCalling;
using AzureAI.FunctionCalling.Abstractions;
using FluentAssertions;
using Moq;

namespace AzureAI.FunctionCalling.Tests;

public sealed class ToolRegistryTests
{
    private static IToolDefinition MakeTool(string name, string description = "desc")
    {
        var mock = new Mock<IToolDefinition>();
        mock.SetupGet(t => t.Name).Returns(name);
        mock.SetupGet(t => t.Description).Returns(description);
        mock.SetupGet(t => t.ParameterSchema).Returns("{}");
        return mock.Object;
    }

    [Fact]
    public void Register_then_GetTool_returns_tool()
    {
        var registry = new ToolRegistry();
        var tool     = MakeTool("weather");

        registry.Register(tool);

        registry.GetTool("weather").Should().BeSameAs(tool);
    }

    [Fact]
    public void GetTool_unknown_name_returns_null()
    {
        var registry = new ToolRegistry();

        registry.GetTool("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetAllTools_returns_all_registered_tools()
    {
        var registry = new ToolRegistry();
        registry.Register(MakeTool("tool_a"));
        registry.Register(MakeTool("tool_b"));
        registry.Register(MakeTool("tool_c"));

        registry.GetAllTools().Should().HaveCount(3);
    }

    [Fact]
    public void GetAllowedTools_with_null_returns_all_tools()
    {
        var registry = new ToolRegistry();
        registry.Register(MakeTool("tool_a"));
        registry.Register(MakeTool("tool_b"));

        var result = registry.GetAllowedTools(null);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetAllowedTools_with_names_returns_only_matching_tools()
    {
        var registry = new ToolRegistry();
        registry.Register(MakeTool("tool_a"));
        registry.Register(MakeTool("tool_b"));
        registry.Register(MakeTool("tool_c"));

        var allowed = new HashSet<string> { "tool_a", "tool_c" };
        var result  = registry.GetAllowedTools(allowed);

        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().BeEquivalentTo(["tool_a", "tool_c"]);
    }
}
