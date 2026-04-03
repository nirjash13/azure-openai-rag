using System.Text.RegularExpressions;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>Extracts readable text from HTML documents by stripping markup and decoding entities.</summary>
public sealed class HtmlDocumentParser : IDocumentParser
{
    private static readonly Regex ScriptOrStyle = new(
        @"<(script|style)[^>]*>.*?</(script|style)>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex Tags = new(@"<[^>]+>", RegexOptions.Compiled);

    private static readonly Regex Whitespace = new(@"\s{2,}", RegexOptions.Compiled);

    public DocumentType SupportedType => DocumentType.Html;

    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        using var reader = new StreamReader(fileStream, leaveOpen: true);
        var html = await reader.ReadToEndAsync(cancellationToken);

        var text = ScriptOrStyle.Replace(html, " ");
        text     = Tags.Replace(text, " ");
        text     = System.Net.WebUtility.HtmlDecode(text);
        text     = Whitespace.Replace(text, " ");

        return text.Trim();
    }
}
