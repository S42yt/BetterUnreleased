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
            string playlistFolder = GetPlaylistFolder(playlistId);
            string fileName = Path.GetFileName(sourceFile);
            string destination = Path.Combine(playlistFolder, fileName);
            File.Copy(sourceFile, destination, true);
            return destination;
        }
    }
}