using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using BetterUnreleased.Models;

namespace BetterUnreleased.Views
{
    public partial class EditMusicDialog : Window
    {
        private readonly Song song;
        private string? newThumbnailPath;

        public EditMusicDialog(Song song)
        {
            InitializeComponent();
            this.song = song;
            
            TitleTextBox.Text = song.Title;
            ArtistTextBox.Text = song.Artist;
            
            if (!string.IsNullOrEmpty(song.ThumbnailPath))
            {
                ThumbnailImage.Source = new BitmapImage(new Uri(song.ThumbnailPath));
            }
        }

        private void SelectThumbnail_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                newThumbnailPath = openFileDialog.FileName;
                ThumbnailImage.Source = new BitmapImage(new Uri(newThumbnailPath));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || 
                string.IsNullOrWhiteSpace(ArtistTextBox.Text))
            {
                MessageBox.Show("Title and Artist are required.");
                return;
            }

            song.Title = TitleTextBox.Text;
            song.Artist = ArtistTextBox.Text;
            if (newThumbnailPath != null)
            {
                song.ThumbnailPath = newThumbnailPath;
            }

            DialogResult = true;
        }
    }
}