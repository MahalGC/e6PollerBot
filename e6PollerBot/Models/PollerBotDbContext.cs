using Microsoft.EntityFrameworkCore;

namespace e6PollerBot.Models
{
    public class PollerBotDbContext : DbContext
    {
        public DbSet<e6Post> e6Posts { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=e6PollerBot.TestDb3;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<e6PostSubscription>()
                .HasKey(eps => new { eps.e6PostId, eps.SubscriptionId });

            modelBuilder.Entity<e6PostSubscription>()
                .HasOne(eps => eps.e6Post)
                .WithMany(ep => ep.e6PostSubscriptions)
                .HasForeignKey(eps2 => eps2.e6PostId);

            modelBuilder.Entity<e6PostSubscription>()
                .HasOne(eps => eps.e6Post)
                .WithMany(s => s.e6PostSubscriptions)
                .HasForeignKey(eps2 => eps2.SubscriptionId);
        }
    }
}
