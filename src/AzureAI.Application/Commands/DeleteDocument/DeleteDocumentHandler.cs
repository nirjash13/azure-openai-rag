using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureAI.Application.Commands.DeleteDocument;

/// <summary>Removes a document from the search index, chunk store, and document store.</summary>
public sealed class DeleteDocumentHandler : IRequestHandler<DeleteDocumentCommand, bool>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ILogger<DeleteDocumentHandler> _logger;

    /// <summary>Initializes a new <see cref="DeleteDocumentHandler"/>.</summary>
    public DeleteDocumentHandler(
        IDocumentRepository documentRepository,
        IChunkRepository chunkRepository,
        IVectorSearchService vectorSearchService,
        ILogger<DeleteDocumentHandler> logger)
    {
        _documentRepository  = documentRepository;
        _chunkRepository     = chunkRepository;
        _vectorSearchService = vectorSearchService;
        _logger              = logger;
    }

    /// <inheritdoc />
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
        {
            _logger.LogWarning("Delete requested for non-existent document {DocumentId}", request.DocumentId);
            return false;
        }

        await _vectorSearchService.RemoveDocumentChunksAsync(request.DocumentId, cancellationToken);
        await _chunkRepository.DeleteByDocumentIdAsync(request.DocumentId, cancellationToken);
        await _documentRepository.DeleteAsync(request.DocumentId, cancellationToken);

        _logger.LogInformation("Deleted document {DocumentId} ('{FileName}')", document.Id, document.FileName);
        return true;
    }
}
