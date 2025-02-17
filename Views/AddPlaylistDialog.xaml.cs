using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using BetterUnreleased.Models;

namespace BetterUnreleased.Views
{
    public partial class AddPlaylistDialog : Window
    {
        private string? selectedThumbnailPath;
        public Playlist? CreatedPlaylist { get; private set; }

        public AddPlaylistDialog()
        {
            InitializeComponent();
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

            CreatedPlaylist = new Playlist
            {
                Title = TitleTextBox.Text,
                ThumbnailPath = selectedThumbnailPath
            };

            DialogResult = true;
        }
    }
}