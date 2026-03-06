using Microsoft.EntityFrameworkCore;
using SearchService.Api.Model;
namespace SearchService.Api.DbContexts
{
    public class SearchDbContext : DbContext
    {
        public SearchDbContext(DbContextOptions<SearchDbContext> options) : base(options)
        {
        }
        public DbSet<UserReadMarker> UserReadMarkers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserReadMarker>().HasKey(x => new { x.UserId, x.ConversationId });
            modelBuilder.Entity<UserReadMarker>()
            .ToTable("UserReadMarkers")
            .HasKey(x => new { x.UserId, x.ConversationId });
        }
    }
}