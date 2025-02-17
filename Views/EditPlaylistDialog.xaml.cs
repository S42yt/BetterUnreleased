using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using BetterUnreleased.Models;

namespace BetterUnreleased.Views
{
    public partial class EditPlaylistDialog : Window
    {
        private Playlist playlist;
        private string? selectedThumbnailPath;

        public EditPlaylistDialog(Playlist playlist)
        {
            InitializeComponent();
            this.playlist = playlist;
            TitleTextBox.Text = playlist.Title;
            if (!string.IsNullOrEmpty(playlist.ThumbnailPath))
            {
                ThumbnailImage.Source = new BitmapImage(new Uri(playlist.ThumbnailPath));
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
                selectedThumbnailPath = openFileDialog.FileName;
                ThumbnailImage.Source = new BitmapImage(new Uri(selectedThumbnailPath));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Please enter a playlist title.");
                return;
            }
            
            playlist.Title = TitleTextBox.Text;
            if (selectedThumbnailPath != null)
            {
                playlist.ThumbnailPath = selectedThumbnailPath;
            }
            
            DialogResult = true;
        }
    }
}