using AzureAI.Application.DTOs;
using MediatR;

namespace AzureAI.Application.Queries.GetDocuments;

/// <summary>Returns the document with the specified identifier, or <c>null</c> if not found.</summary>
public sealed record GetDocumentByIdQuery(Guid DocumentId) : IRequest<DocumentDto?>;
