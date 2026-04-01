using AzureAI.Application.DTOs;

namespace AzureAI.Application.Queries.AskQuestion;

/// <summary>Result of the RAG pipeline for a single user question.</summary>
public sealed record AskQuestionResponse(
    string Answer,
    List<SourceCitationDto> Sources,
    TokenUsageDto TokenUsage,
    long ProcessingTimeMs,
    Guid? ConversationId);
