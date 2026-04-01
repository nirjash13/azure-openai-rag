using System.Diagnostics;
using AzureAI.Application.DTOs;
using AzureAI.Application.Mapping;
using AzureAI.Core.Configuration;
using AzureAI.Core.Domain.Enums;
using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureAI.Application.Queries.AskQuestion;

/// <summary>Executes the RAG pipeline: embed question → retrieve → compose context → generate answer.</summary>
public sealed class AskQuestionHandler : IRequestHandler<AskQuestionQuery, AskQuestionResponse>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ICompletionService _completionService;
    private readonly IConversationRepository _conversationRepository;
    private readonly RagSettings _ragSettings;
    private readonly ILogger<AskQuestionHandler> _logger;

    /// <summary>Initializes a new <see cref="AskQuestionHandler"/>.</summary>
    public AskQuestionHandler(
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearchService,
        ICompletionService completionService,
        IConversationRepository conversationRepository,
        IOptions<RagSettings> ragSettings,
        ILogger<AskQuestionHandler> logger)
    {
        _embeddingService       = embeddingService;
        _vectorSearchService    = vectorSearchService;
        _completionService      = completionService;
        _conversationRepository = conversationRepository;
        _ragSettings            = ragSettings.Value;
        _logger                 = logger;
    }

    /// <inheritdoc />
    public async Task<AskQuestionResponse> Handle(AskQuestionQuery request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);

        var topK    = request.TopK ?? 5;
        var results = await _vectorSearchService.SearchAsync(queryEmbedding, topK, minimumScore: 0.0, cancellationToken);

        _logger.LogInformation("Retrieved {Count} chunks for question", results.Count);

        var includeCitations = request.IncludeCitations ?? _ragSettings.IncludeCitations;
        var contextWindow    = BuildContextWindow(results, _ragSettings.MaxContextTokens);
        var systemPrompt     = BuildSystemPrompt(contextWindow, includeCitations);

        var messages = new List<(ChatRole Role, string Content)>
        {
            (ChatRole.System, systemPrompt)
        };

        if (request.ConversationId.HasValue)
        {
            var history = await _conversationRepository.GetRecentMessagesAsync(
                request.ConversationId.Value,
                _ragSettings.MaxConversationHistory,
                cancellationToken);

            foreach (var msg in history)
                messages.Add((msg.Role, msg.Content));
        }

        messages.Add((ChatRole.User, request.Question));

        var completion = await _completionService.GenerateAsync(messages, cancellationToken);

        if (request.ConversationId.HasValue)
        {
            var userMessage      = new Core.Domain.Entities.ChatMessage(request.ConversationId.Value, ChatRole.User, request.Question);
            var assistantMessage = new Core.Domain.Entities.ChatMessage(request.ConversationId.Value, ChatRole.Assistant, completion.Content);
            await _conversationRepository.AppendMessagesAsync(request.ConversationId.Value, userMessage, assistantMessage);
        }

        sw.Stop();

        var sources = includeCitations
            ? results.Select(r => r.ToCitationDto()).ToList()
            : [];

        return new AskQuestionResponse(
            completion.Content,
            sources,
            completion.TokenUsage.ToDto(),
            sw.ElapsedMilliseconds,
            request.ConversationId);
    }

    private static string BuildContextWindow(IReadOnlyList<SearchResult> results, int maxTokens)
    {
        var sb      = new System.Text.StringBuilder();
        var tokens  = 0;

        foreach (var result in results)
        {
            var header = $"[Source: {result.DocumentName}]\n";
            var entry  = header + result.ContentSnippet + "\n\n";

            // Rough token estimate: chars / 4
            var entryTokens = entry.Length / 4;
            if (tokens + entryTokens > maxTokens)
                break;

            sb.Append(entry);
            tokens += entryTokens;
        }

        return sb.ToString();
    }

    private static string BuildSystemPrompt(string contextWindow, bool includeCitations)
    {
        var citationInstruction = includeCitations
            ? "\nWhen referencing information, cite the source using the [Source: ...] markers provided in the context."
            : string.Empty;

        return $"""
            You are a helpful assistant that answers questions strictly based on the provided context.
            If the answer cannot be found in the context, say so clearly — do not fabricate information.{citationInstruction}

            Context:
            {contextWindow}
            """;
    }
}
