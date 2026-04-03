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
    private readonly AzureSearchSettings _searchSettings;
    private readonly ILogger<AskQuestionHandler> _logger;

    /// <summary>Initializes a new <see cref="AskQuestionHandler"/>.</summary>
    public AskQuestionHandler(
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearchService,
        ICompletionService completionService,
        IConversationRepository conversationRepository,
        IOptions<RagSettings> ragSettings,
        IOptions<AzureSearchSettings> searchSettings,
        ILogger<AskQuestionHandler> logger)
    {
        _embeddingService       = embeddingService;
        _vectorSearchService    = vectorSearchService;
        _completionService      = completionService;
        _conversationRepository = conversationRepository;
        _ragSettings            = ragSettings.Value;
        _searchSettings         = searchSettings.Value;
        _logger                 = logger;
    }

    /// <inheritdoc />
    public async Task<AskQuestionResponse> Handle(AskQuestionQuery request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);

        var topK    = request.TopK ?? _searchSettings.TopK;
        var results = await _vectorSearchService.SearchAsync(queryEmbedding, topK, _searchSettings.MinScore, cancellationToken);

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
            var session = await _conversationRepository.GetByIdAsync(request.ConversationId.Value, cancellationToken);
            if (session is null)
                throw new KeyNotFoundException($"Conversation '{request.ConversationId.Value}' not found.");

            var userMessage      = new Core.Domain.Entities.ChatMessage(request.ConversationId.Value, ChatRole.User, request.Question);
            var assistantMessage = new Core.Domain.Entities.ChatMessage(request.ConversationId.Value, ChatRole.Assistant, completion.Content);
            await _conversationRepository.AppendMessagesAsync(
                request.ConversationId.Value,
                new[] { userMessage, assistantMessage },
                cancellationToken);
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
        var sb     = new System.Text.StringBuilder();
        var tokens = 0;

        foreach (var result in results)
        {
            var citation = result.Metadata.PageNumber.HasValue
                ? $"[Source: {result.DocumentName}, page {result.Metadata.PageNumber}]"
                : $"[Source: {result.DocumentName}]";

            var entry       = $"{citation}\n{result.ContentSnippet}\n\n";
            var entryTokens = entry.Length / 4;
            if (tokens + entryTokens > maxTokens)
                break;

            sb.Append(entry);
            tokens += entryTokens;
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildSystemPrompt(string contextWindow, bool includeCitations)
    {
        var citationInstruction = includeCitations
            ? "\nCite your sources using [Source: <document name>] notation after each claim."
            : string.Empty;

        return $"""
            You are a precise and helpful assistant. Answer the user's question using ONLY the information in the CONTEXT section below.
            If the context does not contain sufficient information, say so explicitly — do not invent or infer facts.
            Be concise and accurate. Use bullet points for multi-part answers.{citationInstruction}

            CONTEXT:
            {contextWindow}
            """;
    }
}
