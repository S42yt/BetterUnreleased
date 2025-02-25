using Microsoft.EntityFrameworkCore;
using BetterUnreleased.Models;
using System.IO;
using BetterUnreleased.Helpers;

namespace BetterUnreleased.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Song> Songs { get; set; }
        public DbSet<Playlist> Playlists { get; set; }

        public AppDbContext()
        {
            FileManager.GetBaseFolder();
            
            try
            {
                if (!Database.CanConnect())
                {
                    Database.Migrate();
                }
                else if (Database.GetPendingMigrations().Any())
                {
                    Database.Migrate();
                }

                if (!Playlists.Any())
                {
                    Playlists.Add(new Playlist { Id = 1, Title = "Unreleased", ThumbnailPath = "" });
                    SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize database: {ex.Message}", ex);
            }
        }

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
                
                entity.Property(e => e.PlaylistId)
                      .HasDefaultValue(1);
                
                entity.HasOne<Playlist>()
                      .WithMany(p => p.Songs)
                      .HasForeignKey(e => e.PlaylistId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Playlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
            });
            
            modelBuilder.Entity<Playlist>().HasData(new Playlist { Id = 1, Title = "Unreleased", ThumbnailPath = "" });
        }

        public override int SaveChanges()
        {
            int result = base.SaveChanges();

            foreach (var entry in ChangeTracker.Entries<Playlist>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    string folder = FileManager.GetPlaylistFolder(entry.Entity.Id);
                }
            }
            return result;
        }
    }
}