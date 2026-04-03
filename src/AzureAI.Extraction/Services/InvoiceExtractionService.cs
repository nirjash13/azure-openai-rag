using System.Text;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using AzureAI.Core.Configuration;
using AzureAI.Extraction.Models;
using AzureAI.Extraction.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AzureAI.Extraction.Services;

public sealed class InvoiceExtractionService
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<InvoiceExtractionService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public InvoiceExtractionService(
        IOptions<AzureOpenAISettings> settings,
        ILogger<InvoiceExtractionService> logger)
    {
        _settings    = settings.Value;
        _azureClient = new AzureOpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        _logger      = logger;
    }

    public async Task<ExtractionResult<ExtractedInvoice>> ExtractAsync(
        Stream documentStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var chatClient = _azureClient.GetChatClient(_settings.ChatDeployment);
        var options    = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };

        List<ChatMessage> messages;

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            var imageBytes   = await BinaryData.FromStreamAsync(documentStream, cancellationToken);
            var contentParts = new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(InvoiceExtractionPrompt.Build()),
                ChatMessageContentPart.CreateImagePart(imageBytes, contentType),
            };
            messages = [new UserChatMessage(contentParts)];
        }
        else
        {
            var bytes    = await ReadAllBytesAsync(documentStream, cancellationToken);
            var base64   = Convert.ToBase64String(bytes);
            var userText = $"{InvoiceExtractionPrompt.Build()}\n\nDocument (base64):\n{base64}";
            messages     = [new UserChatMessage(userText)];
        }

        var result     = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var jsonText   = result.Value.Content[0].Text;

        _logger.LogDebug("Invoice extraction raw response length: {Length}", jsonText.Length);

        ExtractedInvoice? invoice;
        try
        {
            invoice = JsonSerializer.Deserialize<ExtractedInvoice>(jsonText, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize invoice extraction response");
            invoice = null;
        }

        if (invoice is null)
        {
            var emptyInvoice = new ExtractedInvoice(
                string.Empty, string.Empty, null,
                DateOnly.MinValue, null,
                0m, 0m, 0m, "USD",
                Array.Empty<ExtractedLineItem>(),
                0.0);

            var failConfidences = new[] { new ExtractionConfidence("Parse", 0.0, "JSON deserialization failed") };
            return new ExtractionResult<ExtractedInvoice>(emptyInvoice, failConfidences, true, 0.0);
        }

        var confidences = BuildDefaultConfidences();
        var overallConfidence = confidences.Average(c => c.Score);
        return new ExtractionResult<ExtractedInvoice>(invoice, confidences, false, overallConfidence);
    }

    private static IReadOnlyList<ExtractionConfidence> BuildDefaultConfidences() =>
    [
        new("InvoiceNumber", 0.9),
        new("VendorName",    0.9),
        new("InvoiceDate",   0.9),
        new("TotalAmount",   0.9),
        new("LineItems",     0.9),
    ];

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
