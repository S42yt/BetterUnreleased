using System.Configuration;
using System.Data;
using System.Windows;
using BetterUnreleased.Data; 

namespace better_unreleased;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        try
        {
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            db.EnsureUnreleasedPlaylistExists();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}

