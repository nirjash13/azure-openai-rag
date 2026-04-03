using AzureAI.FunctionCalling;
using AzureAI.FunctionCalling.Abstractions;
using AzureAI.FunctionCalling.Tools;
using FluentAssertions;
using Moq;

namespace AzureAI.FunctionCalling.Tests;

public sealed class FunctionCallingOrchestratorTests
{
    [Fact]
    public async Task ToolDefinition_ExecuteAsync_is_called_with_correct_arguments()
    {
        const string expectedArgs = """{"location":"Paris"}""";
        var context = new ToolExecutionContext(
            UserId: null,
            AllowedTools: new HashSet<string>(),
            IsDestructiveConfirmed: false);

        var mock = new Mock<IToolDefinition>();
        mock.SetupGet(t => t.Name).Returns("test_tool");
        mock.SetupGet(t => t.Description).Returns("A test tool");
        mock.SetupGet(t => t.ParameterSchema).Returns("{}");
        mock.Setup(t => t.ExecuteAsync(expectedArgs, context, CancellationToken.None))
            .ReturnsAsync("""{"result":"ok"}""");

        await mock.Object.ExecuteAsync(expectedArgs, context, CancellationToken.None);

        mock.Verify(t => t.ExecuteAsync(expectedArgs, context, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateExpenseEntryTool_unconfirmed_context_returns_confirmation_message()
    {
        var tool = new CreateExpenseEntryTool();
        var context = new ToolExecutionContext(
            UserId: null,
            AllowedTools: new HashSet<string>(),
            IsDestructiveConfirmed: false);

        var result = await tool.ExecuteAsync(
            """{"amount":50.00,"category":"Food","description":"Lunch","date":"2024-01-10"}""",
            context,
            CancellationToken.None);

        result.Should().Contain("confirmationRequired");
    }
}
