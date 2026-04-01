using AzureAI.Application.DTOs;
using AzureAI.Application.Mapping;
using AzureAI.Core.Interfaces.Repositories;
using MediatR;

namespace AzureAI.Application.Queries.GetDocuments;

/// <summary>Returns all documents from the document store mapped to DTOs.</summary>
public sealed class GetDocumentsHandler : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IDocumentRepository _documentRepository;

    /// <summary>Initializes a new <see cref="GetDocumentsHandler"/>.</summary>
    public GetDocumentsHandler(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentDto>> Handle(
        GetDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetAllAsync(cancellationToken);
        return documents.Select(d => d.ToDto()).ToList();
    }
}
