using JustOne.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JustOne.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Post> Posts => Set<Post>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<GuestbookEntry> GuestbookEntries => Set<GuestbookEntry>();

        public DbSet<Comment> Comments => Set<Comment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Every lookup filters by these columns - without indexes SQLite scans the whole table.
            builder.Entity<Post>().HasIndex(p => new { p.UserId, p.CreatedAt });
            builder.Entity<RefreshToken>().HasIndex(t => t.Token).IsUnique();
            builder.Entity<RefreshToken>().HasIndex(t => t.UserId);
            builder.Entity<GuestbookEntry>().HasIndex(e => e.UserId);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // SQLite has no time zone info, so DateTimes come back as Kind=Unspecified and are
            // serialized without "Z" - the browser would then treat UTC times as local time.
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        }

        private class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
