using AzureAI.Application.DTOs;
using MediatR;

namespace AzureAI.Application.Queries.GetDocuments;

/// <summary>Query that returns all ingested documents.</summary>
public sealed record GetDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>;
