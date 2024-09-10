using Microsoft.EntityFrameworkCore;

namespace GGOverlay.Models
{
    public class CounterContext : DbContext
    {
        public DbSet<Counter> Counters { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=counter.db");
        }
    }
}
