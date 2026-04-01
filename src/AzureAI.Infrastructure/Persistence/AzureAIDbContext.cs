using AzureAI.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzureAI.Infrastructure.Persistence;

/// <summary>EF Core database context for the Azure AI RAG application.</summary>
public sealed class AzureAIDbContext : DbContext
{
    /// <summary>Initializes a new <see cref="AzureAIDbContext"/>.</summary>
    public AzureAIDbContext(DbContextOptions<AzureAIDbContext> options) : base(options) { }

    /// <summary>Documents that have been submitted for ingestion.</summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>Text chunks extracted from ingested documents.</summary>
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    /// <summary>Multi-turn conversation sessions.</summary>
    public DbSet<ConversationSession> Conversations => Set<ConversationSession>();

    /// <summary>Individual messages within conversation sessions.</summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AzureAIDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
