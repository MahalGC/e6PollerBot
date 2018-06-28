using Microsoft.EntityFrameworkCore;

namespace e6PollerBot.Models
{
    public class DatabaseContext : DbContext
    {
        public DbSet<e6Post> e6Posts { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFGetStarted.ConsoleApp.NewDb;Trusted_Connection=True;");
        }
    }
}
