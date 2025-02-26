using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BetterUnreleased.Data;
using BetterUnreleased.Models;
using BetterUnreleased.Views;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using TagLib;
using Microsoft.EntityFrameworkCore;
using File = System.IO.File;
using System.Net.Http;

namespace BetterUnreleased
{
    public partial class MainWindow : Window
    {
        private readonly MediaPlayer mediaPlayer = new();
        private readonly AppDbContext db = new();
        private bool isPlaying = false;
        private DispatcherTimer progressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        private enum RepeatMode { None, All, SingleTrack }
        private RepeatMode currentRepeatMode = RepeatMode.None;

        private Point startPoint;
        private bool isDragging = false;

        private List<Song> originalPlaylist = new();
        private List<Song> shuffledPlaylist = new();
        private bool isShuffleOn = false;
        private List<Song> playbackOrder = new();

        private Point playlistStartPoint;
        private bool isPlaylistDragging = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                TogglePlayPauseButton = (Button)FindName("TogglePlayPauseButton");
                LoadPlaylists();
                LoadSongs();
                SongList.Drop += SongList_Drop;
                SongList.DragEnter += SongList_DragEnter;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                progressTimer.Tick += ProgressTimer_Tick;
                SongList.PreviewMouseLeftButtonDown += SongList_PreviewMouseLeftButtonDown;

                PlaylistsGrid.PreviewMouseLeftButtonDown += PlaylistsGrid_PreviewMouseLeftButtonDown;
                PlaylistsGrid.PreviewMouseMove += PlaylistsGrid_PreviewMouseMove;
                PlaylistsGrid.Drop += PlaylistsGrid_Drop;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSongs()
        {
            try
            {
                var songs = db.Songs.ToList();
                foreach (var song in songs)
                {
                    if (string.IsNullOrEmpty(song.FilePath) && db.Playlists.Find(song.PlaylistId) is Playlist pl && !string.IsNullOrEmpty(pl.ThumbnailPath))
                    {
                        song.FilePath = pl.ThumbnailPath;
                    }
                }
                SongList.ItemsSource = songs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading songs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPlaylists()
        {
            var playlists = db.Playlists.OrderBy(p => p.Title).ToList();
            PlaylistsGrid.ItemsSource = playlists;
        }

        private void CreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddPlaylistDialog();
            if (dlg.ShowDialog() == true && dlg.CreatedPlaylist != null)
            {
                db.Playlists.Add(dlg.CreatedPlaylist);
                db.SaveChanges();
                LoadPlaylists();
            }
        }

        private void EditPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Playlist playlist)
            {
                var dlg = new EditPlaylistDialog(playlist);
                if (dlg.ShowDialog() == true)
                {
                    db.SaveChanges();
                    LoadPlaylists();
                }
            }
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Playlist playlist)
            {
                if (playlist.Id == 1)
                {
                    MessageBox.Show("The Unreleased playlist cannot be deleted.", "Warning", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete the playlist '{playlist.Title}' and all its songs?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var songsToDelete = db.Songs.Where(s => s.PlaylistId == playlist.Id).ToList();
                        
                        using var transaction = db.Database.BeginTransaction();
                        try
                        {
                            db.Songs.RemoveRange(songsToDelete);
                            
                            db.Playlists.Remove(playlist);
                            
                            db.SaveChanges();
                            transaction.Commit();
                            
                            string playlistFolder = Helpers.FileManager.GetPlaylistFolder(playlist.Id);
                            if (Directory.Exists(playlistFolder))
                            {
                                Directory.Delete(playlistFolder, true);
                            }
                            
                            LoadPlaylists();
                            SongList.ItemsSource = null;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Error deleting playlist from database: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting playlist: {ex.Message}", "Error", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SongList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try 
                {
                    if (PlaylistsGrid.SelectedItem is not Playlist selectedPlaylist)
                    {
                        MessageBox.Show("Please select a playlist first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files == null) return;

                    using var transaction = db.Database.BeginTransaction();
                    try
                    {
                        foreach (string file in files)
                        {
                            if (!file.EndsWith(".mp3") && !file.EndsWith(".wav")) continue;

                            using var tagFile = TagLib.File.Create(file);
                            var song = new Song
                            {
                                Title = !string.IsNullOrEmpty(tagFile.Tag.Title) 
                                    ? tagFile.Tag.Title.Trim() 
                                    : Path.GetFileNameWithoutExtension(file),
                                Artist = !string.IsNullOrEmpty(tagFile.Tag.FirstPerformer) 
                                    ? tagFile.Tag.FirstPerformer.Trim() 
                                    : "Unknown Artist",
                                FilePath = Helpers.FileManager.CopyMusicFileToPlaylist(file, selectedPlaylist.Id),
                                Duration = tagFile.Properties.Duration.TotalSeconds,
                                PlaylistId = selectedPlaylist.Id
                            };

                            if (tagFile.Tag.Pictures.Length > 0)
                            {
                                var picture = tagFile.Tag.Pictures[0];
                                song.ThumbnailPath = GetThumbnailPath(picture, null);
                            }

                            db.Songs.Add(song);
                        }

                        db.SaveChanges();
                        transaction.Commit();
                        LoadSongs();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error adding songs to database: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding songs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (e.Data.GetData(typeof(Song)) is Song droppedSong)
            {
                var targetPosition = e.GetPosition(SongList);
                var targetItem = SongList.InputHitTest(targetPosition) as UIElement;
                if (targetItem != null)
                {
                    var targetSong = (targetItem as FrameworkElement)?.DataContext as Song;
                    if (targetSong != null && droppedSong != targetSong)
                    {
                        var songs = (SongList.ItemsSource as List<Song>) ?? new List<Song>();
                        int removedIdx = songs.IndexOf(droppedSong);
                        int targetIdx = songs.IndexOf(targetSong);

                        if (removedIdx != -1 && targetIdx != -1)
                        {
                            songs.RemoveAt(removedIdx);
                            songs.Insert(targetIdx, droppedSong);
                            
                            SongList.ItemsSource = null;
                            SongList.ItemsSource = songs;
                            SongList.SelectedItem = droppedSong;
                        }
                    }
                }
            }
        }

        private string? GetThumbnailPath(TagLib.IPicture? picture, string? fallbackThumbnailPath)
        {
            if (picture != null && PlaylistsGrid.SelectedItem is Playlist selectedPlaylist)
            {
                try
                {
                    using (var ms = new System.IO.MemoryStream(picture.Data.Data))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));

                        // Create a temporary file
                        string tempThumbnailPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                        using (var fileStream = new System.IO.FileStream(tempThumbnailPath, System.IO.FileMode.Create))
                        {
                            encoder.Save(fileStream);
                        }

                        // Copy to the proper thumbnails folder
                        string permanentPath = Helpers.FileManager.CopyThumbnailToPlaylist(tempThumbnailPath, selectedPlaylist.Id);
                        
                        // Delete temp file after copying
                        if (System.IO.File.Exists(tempThumbnailPath))
                        {
                            System.IO.File.Delete(tempThumbnailPath);
                        }
                        
                        return permanentPath;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error extracting thumbnail: {ex.Message}", "Thumbnail Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return fallbackThumbnailPath;
        }

        private void SongList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void TogglePlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                if (SongList.SelectedItem is Song song)
                {
                    if (mediaPlayer.Source == null)
                    {
                        mediaPlayer.Open(new Uri(song.FilePath));
                        mediaPlayer.Play();
                    }
                    else
                    {
                        mediaPlayer.Play();
                    }
                    isPlaying = true;
                    progressTimer.Start();
                    CurrentSongTitle.Text = song.Title;
                    TogglePlayPauseButton.Content = "⏸";
                }
            }
            else
            {
                mediaPlayer.Pause();
                isPlaying = false;
                progressTimer.Stop();
                TogglePlayPauseButton.Content = "▶";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = SongList.SelectedIndex;
            if (currentIndex > 0)
            {
                SongList.SelectedIndex = currentIndex - 1;
                if (SongList.SelectedItem is Song previousSong)
                {
                    mediaPlayer.Open(new Uri(previousSong.FilePath));
                    mediaPlayer.Play();
                    isPlaying = true;
                    progressTimer.Start();
                    CurrentSongTitle.Text = previousSong.Title;
                    TogglePlayPauseButton.Content = "⏸";
                }
            }
        }

        private void PlaySong(Song song)
        {
            mediaPlayer.Open(new Uri(song.FilePath));
            mediaPlayer.Play();
            isPlaying = true;
            progressTimer.Start();
            CurrentSongTitle.Text = song.Title;
            TogglePlayPauseButton.Content = "⏸";
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            if (isShuffleOn)
            {
                if (SongList.SelectedItem is Song currentSong)
                {
                    int currentIndex = playbackOrder.FindIndex(s => s.Id == currentSong.Id);
                    int nextIndex = currentIndex + 1;
                    if (nextIndex >= playbackOrder.Count)
                    {
                        if (currentRepeatMode == RepeatMode.All)
                            nextIndex = 0; 
                        else 
                            return;
                    }
                    Song nextSong = playbackOrder[nextIndex];
                    SongList.SelectedItem = nextSong;
                    PlaySong(nextSong);
                }
            }
            else
            {
                int currentIndex = SongList.SelectedIndex;
                int nextIndex = currentIndex + 1;
                if (nextIndex >= SongList.Items.Count)
                {
                    if (currentRepeatMode == RepeatMode.All)
                        nextIndex = 0;
                    else
                        return;
                }
                SongList.SelectedIndex = nextIndex;
                if (SongList.SelectedItem is Song nextSong)
                {
                    PlaySong(nextSong);
                }
            }
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            currentRepeatMode = currentRepeatMode switch
            {
                RepeatMode.None => RepeatMode.All,
                RepeatMode.All => RepeatMode.SingleTrack,
                RepeatMode.SingleTrack => RepeatMode.None,
                _ => RepeatMode.None
            };

            RepeatButton.Content = currentRepeatMode switch
            {
                RepeatMode.None => "🔁",
                RepeatMode.All => "🔂",
                RepeatMode.SingleTrack => "🔂1",
                _ => "🔁"
            };

            RepeatButton.ToolTip = currentRepeatMode switch
            {
                RepeatMode.None => "Repeat Off",
                RepeatMode.All => "Repeat Playlist",
                RepeatMode.SingleTrack => "Repeat Track",
                _ => "Repeat Off"
            };
        }

        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                ProgressSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                ProgressSlider.Value = mediaPlayer.Position.TotalSeconds;
                CurrentTime.Text = $"{mediaPlayer.Position:mm\\:ss}/{mediaPlayer.NaturalDuration.TimeSpan:mm\\:ss}";
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ProgressSlider.IsMouseCaptureWithin)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
            }
        }

        private void EditSong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Song selectedSong)
            {
                var dlg = new EditMusicDialog(selectedSong);
                if (dlg.ShowDialog() == true)
                {
                    db.SaveChanges();
                    LoadSongs();
                }
            }
        }

        private void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Song selectedSong)
            {
                db.Songs.Remove(selectedSong);
                db.SaveChanges();
                LoadSongs();
            }
        }

        private void AddMusicButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistsGrid.SelectedItem is not Playlist selectedPlaylist)
            {
                MessageBox.Show("Please select a playlist first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new AddMusicDialog();
            if (dialog.ShowDialog() == true && dialog.CreatedSong != null)
            {
                try
                {
                    using var transaction = db.Database.BeginTransaction();
                    try
                    {
                        string newFilePath = Helpers.FileManager.CopyMusicFileToPlaylist(
                            dialog.CreatedSong.FilePath, 
                            selectedPlaylist.Id);
                        
                        dialog.CreatedSong.FilePath = newFilePath;
                        dialog.CreatedSong.PlaylistId = selectedPlaylist.Id;
                        
                        db.Songs.Add(dialog.CreatedSong);
                        db.SaveChanges();
                        transaction.Commit();
                        
                        PlaylistsGrid_SelectionChanged(PlaylistsGrid, null);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Error adding song to database: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding song: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            switch (currentRepeatMode)
            {
                case RepeatMode.SingleTrack:
                    mediaPlayer.Position = TimeSpan.Zero;
                    mediaPlayer.Play();
                    break;

                case RepeatMode.All:
                    if (isShuffleOn)
                    {
                        if (SongList.SelectedItem is Song currentSong)
                        {
                            int currentIndex = playbackOrder.FindIndex(s => s.Id == currentSong.Id);
                            int nextIndex = currentIndex + 1;
                            if (nextIndex >= playbackOrder.Count)
                            {
                                nextIndex = 0;
                            }
                            Song nextSong = playbackOrder[nextIndex];
                            SongList.SelectedItem = nextSong;
                            PlaySong(nextSong);
                        }
                    }
                    else
                    {
                        var currentIndex = SongList.SelectedIndex;
                        int nextIndex = currentIndex + 1;
                        if (nextIndex >= SongList.Items.Count)
                        {
                            nextIndex = 0;
                        }
                        SongList.SelectedIndex = nextIndex;
                        if (SongList.SelectedItem is Song nextSong)
                        {
                            PlaySong(nextSong);
                        }
                    }
                    break;

                case RepeatMode.None:
                    if (SongList.SelectedIndex < SongList.Items.Count - 1)
                    {
                        SkipButton_Click(sender, new RoutedEventArgs());
                    }
                    else
                    {
                        isPlaying = false;
                        TogglePlayPauseButton.Content = "▶";
                        mediaPlayer.Stop();
                    }
                    break;
            }
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistsGrid.SelectedItem is not Playlist selectedPlaylist)
            {
                MessageBox.Show("Please select a playlist to shuffle.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            originalPlaylist = db.Songs
                .Where(s => s.PlaylistId == selectedPlaylist.Id)
                .ToList();
            
            if (!isShuffleOn)
            {
                isShuffleOn = true;
                playbackOrder = originalPlaylist.OrderBy(x => Guid.NewGuid()).ToList();
                ShuffleButton.Background = new SolidColorBrush(Colors.LightBlue);
            }
            else
            {
                isShuffleOn = false;
                playbackOrder.Clear();
                ShuffleButton.ClearValue(Button.BackgroundProperty);
            }
        }

        private void PlaylistsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistsGrid.SelectedItem is Playlist selectedPlaylist)
            {
                var playlistSongs = db.Songs
                    .Where(s => s.PlaylistId == selectedPlaylist.Id)
                    .ToList();

                foreach (var song in playlistSongs)
                {
                    if (string.IsNullOrEmpty(song.ThumbnailPath) && !string.IsNullOrEmpty(selectedPlaylist.ThumbnailPath))
                    {
                        song.ThumbnailPath = selectedPlaylist.ThumbnailPath;
                    }
                }

                SongList.ItemsSource = playlistSongs;
            }
        }

        private void SongList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void ListViewItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
            {
                Point position = e.GetPosition(null);
                
                if (Math.Abs(position.X - startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is ListViewItem item)
                    {
                        var song = item.DataContext as Song;
                        if (song != null)
                        {
                            isDragging = true;
                            DragDrop.DoDragDrop(item, song, DragDropEffects.Move);
                            isDragging = false;
                        }
                    }
                }
            }
        }

        private void ListViewItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListViewItem targetItem)
            {
                var droppedSong = e.Data.GetData(typeof(Song)) as Song;
                var targetSong = targetItem.DataContext as Song;
                
                if (droppedSong != null && targetSong != null && droppedSong != targetSong)
                {
                    var songs = (SongList.ItemsSource as List<Song>) ?? new List<Song>();
                    int removedIdx = songs.IndexOf(droppedSong);
                    int targetIdx = songs.IndexOf(targetSong);

                    if (removedIdx != -1 && targetIdx != -1)
                    {
                        songs.RemoveAt(removedIdx);
                        songs.Insert(targetIdx, droppedSong);
                        
                        SongList.ItemsSource = null;
                        SongList.ItemsSource = songs;
                        
                        SongList.SelectedItem = droppedSong;
                    }
                }
            }
        }

        private void PlaylistsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            playlistStartPoint = e.GetPosition(null);
        }

        private void PlaylistsGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !isPlaylistDragging)
            {
                Point pos = e.GetPosition(null);
                if (Math.Abs(pos.X - playlistStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(pos.Y - playlistStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is ListBoxItem item && item.DataContext is Playlist playlist)
                    {
                        isPlaylistDragging = true;
                        DragDrop.DoDragDrop(item, playlist, DragDropEffects.Move);
                        isPlaylistDragging = false;
                    }
                }
            }
        }

        private void PlaylistsGrid_Drop(object sender, DragEventArgs e)
        {
            if (sender is ListBoxItem targetItem)
            {
                var droppedPlaylist = e.Data.GetData(typeof(Playlist)) as Playlist;
                var targetPlaylist = targetItem.DataContext as Playlist;
                if (droppedPlaylist != null && targetPlaylist != null && droppedPlaylist != targetPlaylist)
                {
                    var playlists = (PlaylistsGrid.ItemsSource as List<Playlist>) ?? new List<Playlist>();
                    int removedIdx = playlists.IndexOf(droppedPlaylist);
                    int targetIdx = playlists.IndexOf(targetPlaylist);
                    if (removedIdx != -1 && targetIdx != -1)
                    {
                        playlists.RemoveAt(removedIdx);
                        playlists.Insert(targetIdx, droppedPlaylist);
                        PlaylistsGrid.ItemsSource = null;
                        PlaylistsGrid.ItemsSource = playlists;
                        PlaylistsGrid.SelectedItem = droppedPlaylist;
                    }
                }
            }
        }
    }
}