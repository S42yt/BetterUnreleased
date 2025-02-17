namespace BetterUnreleased.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? ThumbnailPath { get; set; }

        public List<Song> Songs { get; set; } = new();
    }
}