using AzureAI.Application.DTOs;
using MediatR;

namespace AzureAI.Application.Queries.Anomalies;

public sealed record AnomalyDetectionQuery(
    IReadOnlyList<TransactionRecord> Transactions,
    double SigmaThreshold = 2.0) : IRequest<IReadOnlyList<AnomalyDto>>;

public sealed record TransactionRecord(
    string Description,
    decimal Amount,
    string Category,
    string Date);
