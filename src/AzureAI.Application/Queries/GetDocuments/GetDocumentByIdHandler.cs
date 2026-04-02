using AzureAI.Application.DTOs;
using AzureAI.Application.Mapping;
using AzureAI.Core.Interfaces.Repositories;
using MediatR;

namespace AzureAI.Application.Queries.GetDocuments;

internal sealed class GetDocumentByIdHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
{
    private readonly IDocumentRepository _documents;

    public GetDocumentByIdHandler(IDocumentRepository documents)
    {
        _documents = documents;
    }

    public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await _documents.GetByIdAsync(request.DocumentId, cancellationToken);
        return document?.ToDto();
    }
}
