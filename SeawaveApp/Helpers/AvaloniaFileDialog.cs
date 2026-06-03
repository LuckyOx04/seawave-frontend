using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace SeawaveApp.Helpers;

public class AvaloniaFileDialog : IFileDialog
{
    private IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }
        
        return null;
    }
    
    public async Task<string[]> SelectPathsAsync(bool isSingleFile)
    {
        var provider = GetStorageProvider();
        if (provider == null)
        {
            return [];
        }

        var singleFileTitle = "Select music track";
        var multipleFilesTitle = "Select Music tracks or folders";
        
        var multipleFilesAudioFilter = new FilePickerFileType("Supported Audio Formats")
        {
            Patterns = ["*.mp3", "*.flac", "*.wav", "*.m4a", "*.aac", "*.ogg", "*.opus", "*.cue"]
        };
        var singleFileAudioFilter = new FilePickerFileType("Supported Audio Formats")
        {
            Patterns = ["*.mp3"]
        };

        var options = new FilePickerOpenOptions
        {
            Title = isSingleFile ? singleFileTitle : multipleFilesTitle,
            AllowMultiple = !isSingleFile,
            FileTypeFilter = isSingleFile ? [singleFileAudioFilter] : [multipleFilesAudioFilter]
        };

        var files = await provider.OpenFilePickerAsync(options);

        return files.Count == 0 ? [] : files.Select(f => f.Path.LocalPath).ToArray();
    }
}