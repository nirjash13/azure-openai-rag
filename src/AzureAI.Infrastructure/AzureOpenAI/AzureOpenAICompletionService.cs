using Azure;
using Azure.AI.OpenAI;
using AzureAI.Core.Configuration;
using AzureAI.Core.Domain.ValueObjects;
using AzureAI.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using CoreChatRole = AzureAI.Core.Domain.Enums.ChatRole;

namespace AzureAI.Infrastructure.AzureOpenAI;

/// <summary>Generates chat completions using Azure OpenAI.</summary>
public sealed class AzureOpenAICompletionService : ICompletionService
{
    private readonly ChatClient _client;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<AzureOpenAICompletionService> _logger;

    /// <summary>Initializes a new <see cref="AzureOpenAICompletionService"/>.</summary>
    public AzureOpenAICompletionService(
        IOptions<AzureOpenAISettings> settings,
        ILogger<AzureOpenAICompletionService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings         = settings.Value;
        var httpClient    = httpClientFactory.CreateClient("AzureOpenAI");
        var clientOptions = new AzureOpenAIClientOptions();
        clientOptions.Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(httpClient);
        var azureClient   = new AzureOpenAIClient(new Uri(_settings.Endpoint), new AzureKeyCredential(_settings.ApiKey), clientOptions);
        _client  = azureClient.GetChatClient(_settings.ChatDeployment);
        _logger  = logger;
    }

    /// <inheritdoc />
    public async Task<CompletionResult> GenerateAsync(
        IReadOnlyList<(CoreChatRole Role, string Content)> messages,
        CancellationToken cancellationToken = default)
    {
        var chatMessages = messages.Select(ToSdkMessage).ToList();

        var options = new ChatCompletionOptions
        {
            MaxOutputTokenCount = _settings.MaxTokens > 0 ? _settings.MaxTokens : null,
            Temperature         = (float)_settings.Temperature,
        };

        var result = await _client.CompleteChatAsync(chatMessages, options, cancellationToken);
        var completion = result.Value;

        var content = completion.Content[0].Text;
        var usage   = TokenUsage.Calculate(
            completion.Usage.InputTokenCount,
            completion.Usage.OutputTokenCount,
            promptCostPerToken: 0.00001m,
            completionCostPerToken: 0.00003m);

        _logger.LogDebug(
            "Completion generated: {PromptTokens} prompt + {CompletionTokens} completion tokens",
            completion.Usage.InputTokenCount,
            completion.Usage.OutputTokenCount);

        return new CompletionResult(content, usage);
    }

    private static ChatMessage ToSdkMessage((CoreChatRole Role, string Content) msg) =>
        msg.Role switch
        {
            CoreChatRole.System    => new SystemChatMessage(msg.Content),
            CoreChatRole.Assistant => new AssistantChatMessage(msg.Content),
            _                      => new UserChatMessage(msg.Content),
        };
}
