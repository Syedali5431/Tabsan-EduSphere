using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Messaging;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Messaging: conversations, the people in them and their messages.

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Subject).IsRequired().HasMaxLength(Conversation.MaxSubjectLength);
        b.HasMany(x => x.Participants).WithOne().HasForeignKey(p => p.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.LastMessageAt).HasDatabaseName("IX_conversations_last_message");
    }
}

internal sealed class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> b)
    {
        b.ToTable("conversation_participants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique().HasDatabaseName("IX_conversation_participants_conversation_user");
        b.HasIndex(x => new { x.UserId, x.ArchivedAt }).HasDatabaseName("IX_conversation_participants_user");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> b)
    {
        b.ToTable("conversation_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Body).IsRequired().HasMaxLength(ConversationMessage.MaxBodyLength);
        b.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ConversationId, x.CreatedAt }).HasDatabaseName("IX_conversation_messages_conversation_time");
    }
}
