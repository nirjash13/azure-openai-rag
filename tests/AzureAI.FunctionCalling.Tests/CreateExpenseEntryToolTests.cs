using AzureAI.FunctionCalling.Abstractions;
using AzureAI.FunctionCalling.Tools;
using FluentAssertions;
using System.Text.Json;

namespace AzureAI.FunctionCalling.Tests;

public sealed class CreateExpenseEntryToolTests
{
    private static readonly ToolExecutionContext ConfirmedContext =
        new(UserId: null, AllowedTools: new HashSet<string>(), IsDestructiveConfirmed: true);

    private static readonly ToolExecutionContext UnconfirmedContext =
        new(UserId: null, AllowedTools: new HashSet<string>(), IsDestructiveConfirmed: false);

    private const string ValidArgs = """{"amount":99.99,"category":"Travel","description":"Flight to NYC","date":"2024-03-15"}""";

    [Fact]
    public async Task ExecuteAsync_unconfirmed_returns_confirmation_required()
    {
        var tool   = new CreateExpenseEntryTool();
        var result = await tool.ExecuteAsync(ValidArgs, UnconfirmedContext, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("confirmationRequired").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_confirmed_returns_created_entry()
    {
        var tool   = new CreateExpenseEntryTool();
        var result = await tool.ExecuteAsync(ValidArgs, ConfirmedContext, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;
        root.GetProperty("status").GetString().Should().Be("created");
        root.GetProperty("category").GetString().Should().Be("Travel");
    }

    [Fact]
    public async Task ExecuteAsync_invalid_json_returns_error()
    {
        var tool   = new CreateExpenseEntryTool();
        var result = await tool.ExecuteAsync("not-json{{{", ConfirmedContext, CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
    }
}
