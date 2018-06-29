using Microsoft.EntityFrameworkCore;
using System;

namespace e6PollerBot.Models
{
    public class PollerBotDbContext : DbContext
    {
        public DbSet<Subscription> Subscriptions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=e6PollerBot.TestDb8;Trusted_Connection=True;");
        }
    }
}
