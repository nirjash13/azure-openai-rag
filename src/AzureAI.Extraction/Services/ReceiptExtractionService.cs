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

public sealed class ReceiptExtractionService
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<ReceiptExtractionService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public ReceiptExtractionService(
        IOptions<AzureOpenAISettings> settings,
        ILogger<ReceiptExtractionService> logger)
    {
        _settings    = settings.Value;
        _azureClient = new AzureOpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey));
        _logger      = logger;
    }

    public async Task<ExtractionResult<ExtractedReceipt>> ExtractAsync(
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
                ChatMessageContentPart.CreateTextPart(ReceiptExtractionPrompt.Build()),
                ChatMessageContentPart.CreateImagePart(imageBytes, contentType),
            };
            messages = [new UserChatMessage(contentParts)];
        }
        else
        {
            var bytes    = await ReadAllBytesAsync(documentStream, cancellationToken);
            var base64   = Convert.ToBase64String(bytes);
            var userText = $"{ReceiptExtractionPrompt.Build()}\n\nDocument (base64):\n{base64}";
            messages     = [new UserChatMessage(userText)];
        }

        var result   = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
        var jsonText = result.Value.Content[0].Text;

        _logger.LogDebug("Receipt extraction raw response length: {Length}", jsonText.Length);

        ExtractedReceipt? receipt;
        try
        {
            receipt = JsonSerializer.Deserialize<ExtractedReceipt>(jsonText, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize receipt extraction response");
            receipt = null;
        }

        if (receipt is null)
        {
            var empty           = new ExtractedReceipt(string.Empty, DateOnly.MinValue, 0m, "USD", Array.Empty<string>());
            var failConfidences = new[] { new ExtractionConfidence("Parse", 0.0, "JSON deserialization failed") };
            return new ExtractionResult<ExtractedReceipt>(empty, failConfidences, true, 0.0);
        }

        var confidences = new[]
        {
            new ExtractionConfidence("VendorName",   0.9),
            new ExtractionConfidence("Date",         0.9),
            new ExtractionConfidence("TotalAmount",  0.9),
        };

        return new ExtractionResult<ExtractedReceipt>(receipt, confidences, false, 0.9);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
