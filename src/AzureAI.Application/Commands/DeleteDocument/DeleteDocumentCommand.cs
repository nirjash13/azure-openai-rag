using MediatR;

namespace AzureAI.Application.Commands.DeleteDocument;

/// <summary>Command that removes a document and all its associated chunks from the system.</summary>
public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest<bool>;
