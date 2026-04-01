using AzureAI.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzureAI.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="DocumentChunk"/> entity.</summary>
internal sealed class ChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.DocumentId).IsRequired();
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.ChunkIndex).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        // EmbeddingVector lives in Azure AI Search — not persisted in the relational store.
        builder.Ignore(c => c.Embedding);

        builder.OwnsOne(c => c.Metadata, metadata =>
        {
            metadata.Property(m => m.StartIndex).HasColumnName("start_index").IsRequired();
            metadata.Property(m => m.EndIndex).HasColumnName("end_index").IsRequired();
            metadata.Property(m => m.PageNumber).HasColumnName("page_number");
            metadata.Property(m => m.Section).HasColumnName("section").HasMaxLength(500);
        });

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.DocumentId);
    }
}
