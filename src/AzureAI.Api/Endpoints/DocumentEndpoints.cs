using AzureAI.Api.Models;
using AzureAI.Application.Commands.DeleteDocument;
using AzureAI.Application.Commands.IngestDocument;
using AzureAI.Application.Queries.GetDocuments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AzureAI.Api.Endpoints;

internal static class DocumentEndpoints
{
    internal static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .WithOpenApi();

        group.MapPost("", UploadDocument)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .Produces<ApiResponse<object>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        group.MapGet("", GetDocuments)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK);

        group.MapGet("{id:guid}", GetDocumentById)
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        group.MapDelete("{id:guid}", DeleteDocument)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> UploadDocument(
        IFormFile file,
        ISender mediator,
        CancellationToken ct)
    {
        var command = new IngestDocumentCommand(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
        var id = await mediator.Send(command, ct);
        var response = ApiResponse.Ok(new { id });
        return Results.Created($"/api/v1/documents/{id}", response);
    }

    private static async Task<IResult> GetDocuments(ISender mediator, CancellationToken ct)
    {
        var documents = await mediator.Send(new GetDocumentsQuery(), ct);
        return Results.Ok(ApiResponse.Ok(documents));
    }

    private static async Task<IResult> GetDocumentById(
        [FromRoute] Guid id,
        ISender mediator,
        CancellationToken ct)
    {
        var document = await mediator.Send(new GetDocumentByIdQuery(id), ct);
        if (document is null)
            return Results.NotFound(ApiResponse.Fail($"Document '{id}' not found."));

        return Results.Ok(ApiResponse.Ok(document));
    }

    private static async Task<IResult> DeleteDocument(
        [FromRoute] Guid id,
        ISender mediator,
        CancellationToken ct)
    {
        var deleted = await mediator.Send(new DeleteDocumentCommand(id), ct);
        if (!deleted)
            return Results.NotFound(ApiResponse.Fail($"Document '{id}' not found."));

        return Results.NoContent();
    }
}
