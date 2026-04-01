using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>Resolves the appropriate <see cref="IDocumentParser"/> for a given <see cref="DocumentType"/>.</summary>
public sealed class DocumentParserFactory
{
    private readonly IReadOnlyDictionary<DocumentType, IDocumentParser> _parsers;

    /// <summary>Initializes a new <see cref="DocumentParserFactory"/> with all registered parsers.</summary>
    public DocumentParserFactory(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.SupportedType);
    }

    /// <summary>Returns the parser for the specified <paramref name="documentType"/>.</summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when no parser is registered for <paramref name="documentType"/>.
    /// </exception>
    public IDocumentParser GetParser(DocumentType documentType)
    {
        if (_parsers.TryGetValue(documentType, out var parser))
            return parser;

        if (_parsers.TryGetValue(DocumentType.Txt, out var fallback))
            return fallback;

        throw new NotSupportedException($"No document parser registered for type '{documentType}'.");
    }
}
