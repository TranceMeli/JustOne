using JustOne.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JustOne.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Post> Posts => Set<Post>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<GuestbookEntry> GuestbookEntries => Set<GuestbookEntry>();

        public DbSet<Comment> Comments => Set<Comment>();
    }
}