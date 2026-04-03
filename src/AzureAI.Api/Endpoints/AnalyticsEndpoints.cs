using AzureAI.Api.Models;
using AzureAI.Application.DTOs;
using AzureAI.Application.Queries.Anomalies;
using AzureAI.Application.Queries.Forecast;
using AzureAI.Application.Queries.Recommendations;
using MediatR;

namespace AzureAI.Api.Endpoints;

internal static class AnalyticsEndpoints
{
    internal static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/analytics/forecast", ForecastAsync)
            .WithTags("Analytics")
            .WithOpenApi()
            .Produces<ApiResponse<IReadOnlyList<ForecastDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/api/v1/analytics/anomalies", DetectAnomaliesAsync)
            .WithTags("Analytics")
            .WithOpenApi()
            .Produces<ApiResponse<IReadOnlyList<AnomalyDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/api/v1/analytics/recommendations", GetRecommendationsAsync)
            .WithTags("Analytics")
            .WithOpenApi()
            .Produces<ApiResponse<IReadOnlyList<RecommendationDto>>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> ForecastAsync(ForecastQuery query, ISender mediator, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Results.Ok(ApiResponse.Ok(result));
    }

    private static async Task<IResult> DetectAnomaliesAsync(AnomalyDetectionQuery query, ISender mediator, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Results.Ok(ApiResponse.Ok(result));
    }

    private static async Task<IResult> GetRecommendationsAsync(BudgetRecommendationQuery query, ISender mediator, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Results.Ok(ApiResponse.Ok(result));
    }
}
