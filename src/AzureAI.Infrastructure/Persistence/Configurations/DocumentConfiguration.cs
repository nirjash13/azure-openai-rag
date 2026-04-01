using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzureAI.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for the <see cref="Document"/> entity.</summary>
internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(500);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(255);
        builder.Property(d => d.FileSize).IsRequired();
        builder.Property(d => d.ChunkCount).IsRequired();
        builder.Property(d => d.FailureReason).HasMaxLength(2000);
        builder.Property(d => d.CreatedAt).IsRequired();

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<DocumentType>(v))
            .HasMaxLength(50);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<IngestionStatus>(v))
            .HasMaxLength(50);
    }
}
