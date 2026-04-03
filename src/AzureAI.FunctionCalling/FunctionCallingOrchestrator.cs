using Azure;
using Azure.AI.OpenAI;
using AzureAI.Core.Configuration;
using AzureAI.FunctionCalling.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace AzureAI.FunctionCalling;

public sealed class FunctionCallingOrchestrator
{
    private const int DefaultMaxIterations = 10;

    private readonly ChatClient _chatClient;
    private readonly ToolRegistry _toolRegistry;
    private readonly ILogger<FunctionCallingOrchestrator> _logger;

    public FunctionCallingOrchestrator(
        IOptions<AzureOpenAISettings> settings,
        ToolRegistry toolRegistry,
        ILogger<FunctionCallingOrchestrator> logger)
    {
        var s = settings.Value;
        var azureClient = new AzureOpenAIClient(new Uri(s.Endpoint), new AzureKeyCredential(s.ApiKey));
        _chatClient   = azureClient.GetChatClient(s.ChatDeployment);
        _toolRegistry = toolRegistry;
        _logger       = logger;
    }

    public async Task<AgentResponse> RunAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var messages     = new List<ChatMessage> { new UserChatMessage(request.Message) };
        var allowedTools = _toolRegistry.GetAllowedTools(request.AllowedTools);
        var toolCallRecords = new List<ToolCallRecord>();

        var options = new ChatCompletionOptions();
        foreach (var tool in allowedTools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name,
                tool.Description,
                BinaryData.FromString(tool.ParameterSchema)));
        }

        var context = new ToolExecutionContext(
            request.UserId,
            request.AllowedTools ?? new HashSet<string>());

        for (int iteration = 0; iteration < DefaultMaxIterations; iteration++)
        {
            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var value = completion.Value;

            _logger.LogInformation("Agent iteration {Iteration}, finish reason: {Reason}",
                iteration + 1, value.FinishReason);

            if (value.FinishReason == ChatFinishReason.Stop)
            {
                var content = value.Content.FirstOrDefault()?.Text ?? string.Empty;
                return new AgentResponse(content, toolCallRecords, iteration + 1);
            }

            if (value.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(value));

                foreach (var toolCall in value.ToolCalls)
                {
                    var tool = _toolRegistry.GetTool(toolCall.FunctionName);
                    if (tool is null)
                    {
                        _logger.LogWarning("Tool '{ToolName}' not found in registry", toolCall.FunctionName);
                        messages.Add(new ToolChatMessage(toolCall.Id, $"Error: tool '{toolCall.FunctionName}' not found."));
                        continue;
                    }

                    var args   = toolCall.FunctionArguments.ToString();
                    var result = await tool.ExecuteAsync(args, context, cancellationToken);
                    toolCallRecords.Add(new ToolCallRecord(toolCall.FunctionName, args, result));
                    messages.Add(new ToolChatMessage(toolCall.Id, result));

                    _logger.LogInformation("Executed tool '{ToolName}'", toolCall.FunctionName);
                }

                continue;
            }

            break;
        }

        throw new InvalidOperationException($"Agent exceeded {DefaultMaxIterations} iterations without completing.");
    }
}
