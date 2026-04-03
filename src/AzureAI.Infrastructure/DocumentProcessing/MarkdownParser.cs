using System.Text.RegularExpressions;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Services;

namespace AzureAI.Infrastructure.DocumentProcessing;

/// <summary>
/// Extracts plain text from Markdown files by stripping common Markdown syntax.
/// Headings, bold/italic markers, code fences, links, and images are simplified to their text content.
/// </summary>
public sealed class MarkdownParser : IDocumentParser
{
    // Fenced code blocks → keep the code content
    private static readonly Regex CodeFence    = new(@"```[^\n]*\n(.*?)```", RegexOptions.Compiled | RegexOptions.Singleline);
    // Inline code → keep the text
    private static readonly Regex InlineCode   = new(@"`([^`]+)`", RegexOptions.Compiled);
    // ATX headings (# Heading) → keep heading text
    private static readonly Regex Heading      = new(@"^#{1,6}\s+", RegexOptions.Compiled | RegexOptions.Multiline);
    // Bold/italic markers
    private static readonly Regex BoldItalic   = new(@"(\*{1,3}|_{1,3})(.*?)\1", RegexOptions.Compiled);
    // Links [text](url) → text
    private static readonly Regex Link         = new(@"\[([^\]]+)\]\([^)]+\)", RegexOptions.Compiled);
    // Images ![alt](url) → alt text
    private static readonly Regex Image        = new(@"!\[([^\]]*)\]\([^)]+\)", RegexOptions.Compiled);
    // Blockquote markers
    private static readonly Regex Blockquote   = new(@"^>\s?", RegexOptions.Compiled | RegexOptions.Multiline);
    // Horizontal rules
    private static readonly Regex HorizontalRule = new(@"^[-*_]{3,}\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    public DocumentType SupportedType => DocumentType.Markdown;

    public async Task<string> ExtractTextAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        using var reader = new StreamReader(fileStream, leaveOpen: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        text = CodeFence.Replace(text, m => m.Groups[1].Value);
        text = InlineCode.Replace(text, m => m.Groups[1].Value);
        text = Heading.Replace(text, string.Empty);
        text = BoldItalic.Replace(text, m => m.Groups[2].Value);
        text = Image.Replace(text, m => m.Groups[1].Value);
        text = Link.Replace(text, m => m.Groups[1].Value);
        text = Blockquote.Replace(text, string.Empty);
        text = HorizontalRule.Replace(text, string.Empty);

        return text.Trim();
    }
}
