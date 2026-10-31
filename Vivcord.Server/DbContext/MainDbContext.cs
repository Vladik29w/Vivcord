using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vivcord.Server.Models;

namespace Vivcord.Server.DbContext;

public class MainDbContext(DbContextOptions<MainDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Role seeding
        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid> { Id = Guid.Parse("fab4fac1-c546-41de-aebc-a17da9526085"), Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "1" },
            new IdentityRole<Guid> { Id = Guid.Parse("c7b013f0-5201-4317-bcc8-c21ff591658d"), Name = "User", NormalizedName = "USER", ConcurrencyStamp = "2" }
        );

        // Conversation
        builder.Entity<Conversation>(entity =>
        {
            entity.Property(c => c.Id)
                .ValueGeneratedOnAdd()
                .HasValueGenerator<SnowflakeValueGenerator>()
                .Metadata.SetValueGenerationStrategy(SqlServerValueGenerationStrategy.None);

            entity.Property(c => c.DirectPairKey)
                .HasMaxLength(73)
                .IsUnicode(false);

            entity.HasIndex(c => c.DirectPairKey)
                .IsUnique()
                .HasFilter("[DirectPairKey] IS NOT NULL");
        });

        // ConversationMember
        builder.Entity<ConversationMember>(entity =>
        {
            entity.HasKey(cm => new { cm.ConversationId, cm.UserId });

            entity.HasIndex(cm => new { cm.UserId, cm.Status });

            entity.HasOne(cm => cm.Conversation)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cm => cm.User)
                .WithMany(u => u.ConversationMemberships)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message
        builder.Entity<Message>(entity =>
        {
            entity.Property(m => m.Id)
                .ValueGeneratedOnAdd()
                .HasValueGenerator<SnowflakeValueGenerator>()
                .Metadata.SetValueGenerationStrategy(SqlServerValueGenerationStrategy.None);

            entity.HasIndex(m => new { m.ConversationId, m.Id })
                .IsDescending(false, true);

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // RefreshToken
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
