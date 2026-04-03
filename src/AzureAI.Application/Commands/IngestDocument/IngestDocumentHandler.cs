using AzureAI.Core.Configuration;
using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Domain.Exceptions;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAI.Application.Commands.IngestDocument;

/// <summary>Orchestrates the full document ingestion pipeline: parse → chunk → embed → index → persist.</summary>
public sealed class IngestDocumentHandler : IRequestHandler<IngestDocumentCommand, Guid>
{
    private const int EmbeddingBatchSize = 16;

    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITextChunker _textChunker;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IEnumerable<IDocumentParser> _parsers;
    private readonly ChunkingSettings _chunkingSettings;
    private readonly ILogger<IngestDocumentHandler> _logger;

    /// <summary>Initializes a new <see cref="IngestDocumentHandler"/>.</summary>
    public IngestDocumentHandler(
        IDocumentRepository documentRepository,
        IChunkRepository chunkRepository,
        IEmbeddingService embeddingService,
        ITextChunker textChunker,
        IVectorSearchService vectorSearchService,
        IEnumerable<IDocumentParser> parsers,
        IOptions<ChunkingSettings> chunkingSettings,
        ILogger<IngestDocumentHandler> logger)
    {
        _documentRepository = documentRepository;
        _chunkRepository     = chunkRepository;
        _embeddingService    = embeddingService;
        _textChunker         = textChunker;
        _vectorSearchService = vectorSearchService;
        _parsers             = parsers;
        _chunkingSettings    = chunkingSettings.Value;
        _logger              = logger;
    }

    /// <inheritdoc />
    public async Task<Guid> Handle(IngestDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentType = ResolveDocumentType(request.ContentType, request.FileName);
        var document     = new Document(request.FileName, request.ContentType, request.FileSize, documentType);

        await _documentRepository.AddAsync(document, cancellationToken);
        _logger.LogInformation("Created document record {DocumentId} for '{FileName}'", document.Id, document.FileName);

        try
        {
            document.MarkProcessing();
            await _documentRepository.UpdateAsync(document, cancellationToken);

            var parser      = ResolveParser(documentType, document.Id);
            var rawText     = await parser.ExtractTextAsync(request.FileStream, cancellationToken);
            var textChunks  = _textChunker.ChunkText(rawText, _chunkingSettings.ChunkSizeTokens, _chunkingSettings.OverlapTokens);

            var limitedChunks = textChunks.Count > _chunkingSettings.MaxChunksPerDocument
                ? textChunks.Take(_chunkingSettings.MaxChunksPerDocument).ToList()
                : textChunks.ToList();

            _logger.LogInformation("Document {DocumentId} produced {ChunkCount} chunks", document.Id, limitedChunks.Count);

            var documentChunks = new List<DocumentChunk>(limitedChunks.Count);

            for (int batchStart = 0; batchStart < limitedChunks.Count; batchStart += EmbeddingBatchSize)
            {
                var batch      = limitedChunks.Skip(batchStart).Take(EmbeddingBatchSize).ToList();
                var batchTexts = batch.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateBatchEmbeddingsAsync(batchTexts, cancellationToken);

                for (int i = 0; i < batch.Count; i++)
                {
                    var chunk    = batch[i];
                    var metadata = new ChunkMetadata(chunk.StartIndex, chunk.EndIndex, chunk.PageNumber, chunk.Section);
                    documentChunks.Add(new DocumentChunk(document.Id, document.FileName, chunk.Content, embeddings[i], metadata, chunk.ChunkIndex));
                }
            }

            await _chunkRepository.AddRangeAsync(documentChunks, cancellationToken);
            await _vectorSearchService.IndexChunksAsync(documentChunks, cancellationToken);

            document.MarkCompleted(documentChunks.Count);
            await _documentRepository.UpdateAsync(document, cancellationToken);

            _logger.LogInformation("Ingestion complete for document {DocumentId}", document.Id);
            return document.Id;
        }
        catch (Exception ex) when (ex is not DocumentProcessingException)
        {
            _logger.LogError(ex, "Ingestion failed for document {DocumentId}", document.Id);
            document.MarkFailed(ex.Message);
            await _documentRepository.UpdateAsync(document, cancellationToken);
            throw new DocumentProcessingException(document.Id, ex.Message, ex);
        }
    }

    private static DocumentType ResolveDocumentType(string contentType, string fileName)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => DocumentType.Pdf,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => DocumentType.Docx,
            "text/html" => DocumentType.Html,
            "text/markdown" => DocumentType.Markdown,
            _ => Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf"  => DocumentType.Pdf,
                ".docx" => DocumentType.Docx,
                ".md"   => DocumentType.Markdown,
                ".html" or ".htm" => DocumentType.Html,
                _ => DocumentType.Txt
            }
        };
    }

    private IDocumentParser ResolveParser(DocumentType documentType, Guid documentId)
    {
        var parser = _parsers.FirstOrDefault(p => p.SupportedType == documentType)
            ?? _parsers.FirstOrDefault(p => p.SupportedType == DocumentType.Txt);

        if (parser is null)
            throw new DocumentProcessingException(documentId,
                $"No document parser registered for type '{documentType}'.");

        return parser;
    }
}
