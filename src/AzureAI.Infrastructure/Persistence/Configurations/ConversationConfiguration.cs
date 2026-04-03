using AzureAI.Core.Domain.Entities;
using AzureAI.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzureAI.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="ConversationSession"/> and <see cref="ChatMessage"/>.</summary>
internal sealed class ConversationConfiguration : IEntityTypeConfiguration<ConversationSession>
{
    public void Configure(EntityTypeBuilder<ConversationSession> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Title).HasMaxLength(500);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.LastMessageAt).IsRequired();

        // Messages are stored via a separate ChatMessage configuration.
        builder.HasMany(c => c.Messages)
            .WithOne()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Messages).HasField("_messages");
    }
}

/// <summary>EF Core configuration for the <see cref="ChatMessage"/> record.</summary>
internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.ConversationId).IsRequired();
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<ChatRole>(v))
            .HasMaxLength(50);

        builder.HasIndex(m => m.ConversationId);
    }
}
