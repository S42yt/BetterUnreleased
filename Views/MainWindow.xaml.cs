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
using TagLib;
using Microsoft.EntityFrameworkCore;

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

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                LoadSongs();
                SongList.Drop += SongList_Drop;
                SongList.DragEnter += SongList_DragEnter;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                progressTimer.Tick += ProgressTimer_Tick;
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
                var songs = db.Songs.OrderBy(s => s.Title).ToList();
                SongList.ItemsSource = songs;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading songs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SongList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try 
                {
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
                                FilePath = file,
                                Duration = tagFile.Properties.Duration.TotalSeconds
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
        }

        private string? GetThumbnailPath(TagLib.IPicture? picture, string? fallbackThumbnailPath)
        {
            if (picture != null)
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

                    var thumbnailPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                    using (var fileStream = new System.IO.FileStream(thumbnailPath, System.IO.FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }

                    return thumbnailPath;
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

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = SongList.SelectedIndex;
            if (currentIndex < SongList.Items.Count - 1)
            {
                SongList.SelectedIndex = currentIndex + 1;
                if (SongList.SelectedItem is Song nextSong)
                {
                    mediaPlayer.Open(new Uri(nextSong.FilePath));
                    mediaPlayer.Play();
                    isPlaying = true;
                    progressTimer.Start();
                    CurrentSongTitle.Text = nextSong.Title;
                    TogglePlayPauseButton.Content = "⏸";
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
            var dialog = new AddMusicDialog();
            if (dialog.ShowDialog() == true && dialog.CreatedSong != null)
            {
                try
                {
                    using var transaction = db.Database.BeginTransaction();
                    try
                    {
                        db.Songs.Add(dialog.CreatedSong);
                        
                        db.SaveChanges();
                        transaction.Commit();
                        
                        LoadSongs();
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
                case RepeatMode.None:
                    var currentIndex = SongList.SelectedIndex;
                    var nextIndex = currentIndex + 1;
                    
                    if (nextIndex < SongList.Items.Count)
                    {
                        SongList.SelectedIndex = nextIndex;
                        if (SongList.SelectedItem is Song nextSong)
                        {
                            mediaPlayer.Open(new Uri(nextSong.FilePath));
                            mediaPlayer.Play();
                            isPlaying = true;
                            CurrentSongTitle.Text = nextSong.Title;
                            TogglePlayPauseButton.Content = "⏸";
                        }
                    }
                    else if (currentRepeatMode == RepeatMode.All && SongList.Items.Count > 0)
                    {
                        SongList.SelectedIndex = 0;
                        if (SongList.SelectedItem is Song firstSong)
                        {
                            mediaPlayer.Open(new Uri(firstSong.FilePath));
                            mediaPlayer.Play();
                            isPlaying = true;
                            CurrentSongTitle.Text = firstSong.Title;
                            TogglePlayPauseButton.Content = "⏸";
                        }
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
    }
}