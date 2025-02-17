using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using BetterUnreleased.Models;

namespace BetterUnreleased.Views
{
    public partial class AddMusicDialog : Window
    {
        private string? selectedFilePath;
        private string? selectedThumbnailPath;

        public Song? CreatedSong { get; private set; }

        public AddMusicDialog()
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

        private void SelectMusic_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePath = openFileDialog.FileName;
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text) || 
                string.IsNullOrWhiteSpace(ArtistTextBox.Text) || 
                string.IsNullOrWhiteSpace(selectedFilePath))
            {
                MessageBox.Show("Please fill in all required fields and select a music file.");
                return;
            }

            var tagFile = TagLib.File.Create(selectedFilePath);
            CreatedSong = new Song
            {
                Title = TitleTextBox.Text,
                Artist = ArtistTextBox.Text,
                FilePath = selectedFilePath,
                ThumbnailPath = selectedThumbnailPath,
                Duration = tagFile.Properties.Duration.TotalSeconds
            };

            DialogResult = true;
        }
    }
}