using AzureAI.FunctionCalling.Abstractions;
using AzureAI.FunctionCalling.Tools;
using FluentAssertions;
using System.Text.Json;

namespace AzureAI.FunctionCalling.Tests;

public sealed class GetWeatherToolTests
{
    private static readonly ToolExecutionContext DefaultContext =
        new(UserId: null, AllowedTools: new HashSet<string>());

    [Fact]
    public async Task ExecuteAsync_returns_valid_json_with_location()
    {
        var tool   = new GetWeatherTool();
        var args   = """{"location":"London"}""";
        var result = await tool.ExecuteAsync(args, DefaultContext, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("location").GetString().Should().Be("London");
    }

    [Fact]
    public async Task ExecuteAsync_returns_temperature_and_condition()
    {
        var tool   = new GetWeatherTool();
        var args   = """{"location":"Berlin"}""";
        var result = await tool.ExecuteAsync(args, DefaultContext, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.TryGetProperty("temperatureCelsius", out _).Should().BeTrue();
        root.TryGetProperty("condition", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_missing_location_uses_default()
    {
        var tool   = new GetWeatherTool();
        var args   = "{}";
        var result = await tool.ExecuteAsync(args, DefaultContext, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("location").GetString().Should().Be("unknown");
    }
}
