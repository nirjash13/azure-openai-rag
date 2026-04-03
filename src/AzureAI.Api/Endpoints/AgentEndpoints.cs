using AzureAI.Api.Models;
using AzureAI.FunctionCalling;
using AzureAI.FunctionCalling.Abstractions;

namespace AzureAI.Api.Endpoints;

internal static class AgentEndpoints
{
    internal static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/agent/chat", AgentChat)
            .WithTags("Agent")
            .WithOpenApi()
            .Produces<ApiResponse<AgentResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapGet("/api/v1/agent/tools", GetTools)
            .WithTags("Agent")
            .WithOpenApi()
            .Produces<ApiResponse<IReadOnlyList<ToolInfo>>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> AgentChat(
        AgentChatRequest request,
        FunctionCallingOrchestrator orchestrator,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest(ApiResponse.Fail("Message is required."));

        var agentRequest = new AgentRequest(request.Message, request.UserId, request.AllowedTools);
        var response     = await orchestrator.RunAsync(agentRequest, ct);

        return Results.Ok(ApiResponse.Ok(response));
    }

    private static IResult GetTools(ToolRegistry registry)
    {
        var tools = registry.GetAllTools()
            .Select(t => new ToolInfo(t.Name, t.Description))
            .ToList();

        return Results.Ok(ApiResponse.Ok<IReadOnlyList<ToolInfo>>(tools));
    }
}

internal sealed record AgentChatRequest(
    string Message,
    string? UserId = null,
    IReadOnlySet<string>? AllowedTools = null);
internal sealed record ToolInfo(string Name, string Description);
