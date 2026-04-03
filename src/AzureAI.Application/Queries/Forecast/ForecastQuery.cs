using AzureAI.Application.DTOs;
using MediatR;

namespace AzureAI.Application.Queries.Forecast;

public sealed record ForecastQuery(
    IReadOnlyList<HistoricalPeriod> HistoricalData,
    int PeriodsToForecast = 3,
    string? Category = null) : IRequest<IReadOnlyList<ForecastDto>>;

public sealed record HistoricalPeriod(
    string Period,
    decimal Amount,
    string? Category = null);
