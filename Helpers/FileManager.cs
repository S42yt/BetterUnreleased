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

        public static string CopyMusicFileToPlaylist(string sourceFile, int playlistId)
        {
            if (!File.Exists(sourceFile))
                throw new FileNotFoundException("Source music file not found", sourceFile);

            string playlistFolder = GetPlaylistFolder(playlistId);
            string fileName = Path.GetFileName(sourceFile);
            string destination = Path.Combine(playlistFolder, fileName);

            int counter = 1;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            while (File.Exists(destination))
            {
                fileName = $"{fileNameWithoutExt}_{counter++}{extension}";
                destination = Path.Combine(playlistFolder, fileName);
            }

            File.Copy(sourceFile, destination, false);
            return destination;
        }
    }
}