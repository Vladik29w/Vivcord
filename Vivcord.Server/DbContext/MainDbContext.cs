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
        public DbSet<UserMessage> UserMessages { get; set; }
        public DbSet<AppUserFriend> UserFriends { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole<Guid>>().HasData(
                new IdentityRole<Guid> { Id = Guid.Parse("fab4fac1-c546-41de-aebc-a17da9526085"), Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "1" },
                new IdentityRole<Guid> { Id = Guid.Parse("c7b013f0-5201-4317-bcc8-c21ff591658d"), Name = "User", NormalizedName = "USER", ConcurrencyStamp = "2" }
            );

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
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
