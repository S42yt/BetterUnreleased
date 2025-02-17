namespace BetterUnreleased.Models
{
    public class Song
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Artist { get; set; }
        public required string FilePath { get; set; }
        public string? ThumbnailPath { get; set; }
        public double Duration { get; set; }
    }
}