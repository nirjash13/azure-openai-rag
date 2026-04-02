using AzureAI.Api.Models;
using AzureAI.Application.Commands.CreateConversation;
using AzureAI.Application.Commands.DeleteConversation;
using AzureAI.Application.DTOs;
using AzureAI.Application.Queries.AskQuestion;
using AzureAI.Application.Queries.GetConversationHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AzureAI.Api.Endpoints;

internal static class ChatEndpoints
{
    internal static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/chat")
            .WithTags("Chat")
            .WithOpenApi();

        group.MapPost("", AskQuestion)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        group.MapPost("conversations", CreateConversation)
            .Produces<ApiResponse<object>>(StatusCodes.Status201Created);

        group.MapGet("{conversationId:guid}", GetConversationHistory)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        group.MapDelete("{conversationId:guid}", DeleteConversation)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> AskQuestion(
        ChatRequestDto request,
        ISender mediator,
        CancellationToken ct)
    {
        var query = new AskQuestionQuery(
            request.Question,
            request.ConversationId,
            request.Options?.TopK,
            request.Options?.IncludeCitations);

        var result = await mediator.Send(query, ct);
        return Results.Ok(ApiResponse.Ok(result));
    }

    private static async Task<IResult> CreateConversation(
        [FromBody] CreateConversationRequest? request,
        ISender mediator,
        CancellationToken ct)
    {
        var id = await mediator.Send(new CreateConversationCommand(request?.Title), ct);
        return Results.Created($"/api/v1/chat/{id}", ApiResponse.Ok(new { id }));
    }

    private static async Task<IResult> GetConversationHistory(
        [FromRoute] Guid conversationId,
        ISender mediator,
        CancellationToken ct)
    {
        var history = await mediator.Send(new GetConversationHistoryQuery(conversationId), ct);
        if (history is null)
            return Results.NotFound(ApiResponse.Fail($"Conversation '{conversationId}' not found."));

        return Results.Ok(ApiResponse.Ok(history));
    }

    private static async Task<IResult> DeleteConversation(
        [FromRoute] Guid conversationId,
        ISender mediator,
        CancellationToken ct)
    {
        var deleted = await mediator.Send(new DeleteConversationCommand(conversationId), ct);
        if (!deleted)
            return Results.NotFound(ApiResponse.Fail($"Conversation '{conversationId}' not found."));

        return Results.NoContent();
    }
}

/// <summary>Request body for creating a new conversation.</summary>
internal sealed record CreateConversationRequest(string? Title);
