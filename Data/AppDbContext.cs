using Microsoft.EntityFrameworkCore;
using BetterUnreleased.Models;
using System.IO;

namespace BetterUnreleased.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string projectPath = Directory.GetCurrentDirectory();
            string databasePath = Path.Combine(projectPath, "Database", "BetterUnreleased.db");
            
            Directory.CreateDirectory(Path.Combine(projectPath, "Database"));
            
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Song>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Artist).IsRequired();
                entity.Property(e => e.FilePath).IsRequired();
                entity.Property(e => e.Duration).IsRequired();
            });
        }
    }
}