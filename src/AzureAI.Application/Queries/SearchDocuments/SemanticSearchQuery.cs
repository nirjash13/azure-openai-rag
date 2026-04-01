using AzureAI.Application.DTOs;
using MediatR;

namespace AzureAI.Application.Queries.SearchDocuments;

/// <summary>Query that performs a vector similarity search and returns ranked results without generating a completion.</summary>
public sealed record SemanticSearchQuery(
    string Query,
    int? TopK,
    double? MinScore) : IRequest<IReadOnlyList<SearchResultDto>>;
