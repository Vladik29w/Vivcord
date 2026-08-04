using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vivcord.Server.Models;

namespace Vivcord.Server.DbContext
{
    public class MainDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        public DbSet<AppUserFriend> UserFriends { get; set; }
        public DbSet<GroupChat> GroupChats { get; set; }
        public DbSet<GroupChatMember> GroupChatMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole<Guid>>().HasData(
                new IdentityRole<Guid> { Id = Guid.Parse("fab4fac1-c546-41de-aebc-a17da9526085"), Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "1" },
                new IdentityRole<Guid> { Id = Guid.Parse("c7b013f0-5201-4317-bcc8-c21ff591658d"), Name = "User", NormalizedName = "USER", ConcurrencyStamp = "2" }
            );

            // AppUserFriend relationships
            builder.Entity<AppUserFriend>()
            .HasKey(uf => new { uf.UserId, uf.FriendId });

            builder.Entity<AppUserFriend>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Friends)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AppUserFriend>()
                .HasOne(uf => uf.Friend)
                .WithMany()
                .HasForeignKey(uf => uf.FriendId)
                .OnDelete(DeleteBehavior.Cascade);

            // GroupChatMember relationships
            builder.Entity<GroupChatMember>()
                .HasKey(gcm => new { gcm.GroupChatId, gcm.UserId });

            builder.Entity<GroupChatMember>()
                .HasOne(gcm => gcm.GroupChat)
                .WithMany(gc => gc.Members)
                .HasForeignKey(gcm => gcm.GroupChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupChatMember>()
                .HasOne(gcm => gcm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gcm => gcm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // GroupChat admin relationship
            builder.Entity<GroupChat>()
                .HasOne(gc => gc.Admin)
                .WithMany(u => u.AdminiedGroups)
                .HasForeignKey(gc => gc.adminId)
                .OnDelete(DeleteBehavior.Restrict);

            // GroupMessage relationship
            builder.Entity<GroupMessage>()
                .HasOne<GroupChat>()
                .WithMany(gc => gc.Messages)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
