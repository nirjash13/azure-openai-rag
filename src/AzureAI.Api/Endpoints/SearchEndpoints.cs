using AzureAI.Api.Models;
using AzureAI.Application.Queries.SearchDocuments;
using MediatR;

namespace AzureAI.Api.Endpoints;

internal static class SearchEndpoints
{
    internal static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/search", SemanticSearch)
            .WithTags("Search")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> SemanticSearch(
        SearchRequest request,
        ISender mediator,
        CancellationToken ct)
    {
        var query = new SemanticSearchQuery(request.Query, request.TopK, request.MinScore);
        var results = await mediator.Send(query, ct);
        return Results.Ok(ApiResponse.Ok(results));
    }
}

/// <summary>Request body for a semantic search query.</summary>
internal sealed record SearchRequest(string Query, int? TopK = null, double? MinScore = null);
