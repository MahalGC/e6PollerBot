using Microsoft.EntityFrameworkCore;
using System;

namespace e6PollerBot.Models
{
    public class e6PollerBotDbContext : DbContext
    {
        public DbSet<e6Subscription> e6Subscriptions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING"));
        }
    }
}
