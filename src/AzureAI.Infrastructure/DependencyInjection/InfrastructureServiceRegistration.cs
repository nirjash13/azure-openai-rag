using AzureAI.Core.Configuration;
using AzureAI.Core.Interfaces.Repositories;
using AzureAI.Core.Interfaces.Services;
using AzureAI.Infrastructure.AzureOpenAI;
using AzureAI.Infrastructure.DocumentProcessing;
using AzureAI.Infrastructure.Persistence;
using AzureAI.Infrastructure.Persistence.Repositories;
using AzureAI.Infrastructure.Resilience;
using AzureAI.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureAI.Infrastructure.DependencyInjection;

/// <summary>Registers all Infrastructure layer services with the DI container.</summary>
public static class InfrastructureServiceRegistration
{
    /// <summary>
    /// Binds configuration options, registers Azure service clients, document processing
    /// components, EF Core persistence, and repository implementations.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAI"));
        services.Configure<AzureSearchSettings>(configuration.GetSection("AzureSearch"));
        services.Configure<ChunkingSettings>(configuration.GetSection("Chunking"));
        services.Configure<RagSettings>(configuration.GetSection("Rag"));

        // Named HTTP clients with Polly resilience policies
        services.AddHttpClient("AzureOpenAI")
            .AddPolicyHandler(RetryPolicies.AzureServiceRetry())
            .AddPolicyHandler(CircuitBreakerPolicies.AzureServiceCircuitBreaker());

        services.AddHttpClient("AzureSearch")
            .AddPolicyHandler(RetryPolicies.AzureServiceRetry())
            .AddPolicyHandler(CircuitBreakerPolicies.AzureServiceCircuitBreaker());

        // Azure OpenAI
        services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();
        services.AddSingleton<ICompletionService, AzureOpenAICompletionService>();

        // Azure AI Search
        services.AddSingleton<IVectorSearchService, AzureAISearchService>();
        services.AddScoped<SearchIndexManager>();

        // Document processing
        services.AddScoped<IDocumentParser, PlainTextParser>();
        services.AddScoped<IDocumentParser, MarkdownParser>();
        services.AddScoped<IDocumentParser, PdfDocumentParser>();
        services.AddScoped<IDocumentParser, DocxDocumentParser>();
        services.AddScoped<IDocumentParser, HtmlDocumentParser>();
        services.AddScoped<ITextChunker, SlidingWindowChunker>();

        // Database
        services.AddDbContext<AzureAIDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        // Repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();

        return services;
    }
}
