using AzureAI.Core.Domain.Enums;

namespace AzureAI.Core.Interfaces.Services;

/// <summary>Extracts plain text from a document file stream.</summary>
public interface IDocumentParser
{
    /// <summary>Gets the document format this parser handles.</summary>
    DocumentType SupportedType { get; }

    /// <summary>Reads <paramref name="fileStream"/> and returns its textual content.</summary>
    /// <param name="fileStream">A readable stream positioned at the start of the document.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default);
}
