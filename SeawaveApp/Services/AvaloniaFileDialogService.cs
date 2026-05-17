using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace SeawaveApp.Services;

public class AvaloniaFileDialogService : IFileDialogService
{
    private IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }
        
        return null;
    }
    
    public async Task<string[]> SelectPathsAsync()
    {
        var provider = GetStorageProvider();
        if (provider == null)
        {
            return [];
        }

        var audioFilter = new FilePickerFileType("Supported Audio Formats")
        {
            Patterns = ["*.mp3", "*.flac", "*.wav", "*.m4a", "*.aac", "*.ogg", "*.opus", "*.cue"]
        };

        var options = new FilePickerOpenOptions
        {
            Title = "Select Music tracks or folders",
            AllowMultiple = true,
            FileTypeFilter = [audioFilter]
        };

        var files = await provider.OpenFilePickerAsync(options);

        return files.Count == 0 ? [] : files.Select(f => f.Path.LocalPath).ToArray();
    }
}