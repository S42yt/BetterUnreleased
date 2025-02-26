using System;
using System.IO;

namespace BetterUnreleased.Helpers
{
    public static class FileManager
    {
        public static string GetBaseFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string baseFolder = Path.Combine(appData, "BetterUnreleased");
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
            return baseFolder;
        }

        public static string GetPlaylistFolder(int playlistId)
        {
            string playlistFolder = Path.Combine(GetBaseFolder(), $"Playlist_{playlistId}");
            if (!Directory.Exists(playlistFolder))
            {
                Directory.CreateDirectory(playlistFolder);
            }
            return playlistFolder;
        }

        public static string GetSongsFolder(int playlistId)
        {
            string playlistFolder = GetPlaylistFolder(playlistId);
            string songsFolder = Path.Combine(playlistFolder, "Songs");
            if (!Directory.Exists(songsFolder))
            {
                Directory.CreateDirectory(songsFolder);
            }
            return songsFolder;
        }

        public static string GetThumbnailsFolder(int playlistId)
        {
            string playlistFolder = GetPlaylistFolder(playlistId);
            string thumbnailsFolder = Path.Combine(playlistFolder, "Thumbnails");
            if (!Directory.Exists(thumbnailsFolder))
            {
                Directory.CreateDirectory(thumbnailsFolder);
            }
            return thumbnailsFolder;
        }

        public static string CopyMusicFileToPlaylist(string sourceFile, int playlistId)
        {
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException("Source music file not found", sourceFile);

            string songsFolder = GetSongsFolder(playlistId);
            string fileName = Path.GetFileName(sourceFile);
            string destination = Path.Combine(songsFolder, fileName);

            int counter = 1;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            while (File.Exists(destination))
            {
                fileName = $"{fileNameWithoutExt}_{counter++}{extension}";
                destination = Path.Combine(songsFolder, fileName);
            }

            File.Copy(sourceFile, destination, false);
            return destination;
        }

        public static string CopyThumbnailToPlaylist(string sourceFile, int playlistId)
        {
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException("Source thumbnail file not found", sourceFile);

            string thumbnailsFolder = GetThumbnailsFolder(playlistId);
            string fileName = Path.GetFileName(sourceFile);
            string destination = Path.Combine(thumbnailsFolder, fileName);

            int counter = 1;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            while (File.Exists(destination))
            {
                fileName = $"{fileNameWithoutExt}_{counter++}{extension}";
                destination = Path.Combine(thumbnailsFolder, fileName);
            }

            File.Copy(sourceFile, destination, false);
            return destination;
        }
    }
}