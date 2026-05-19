using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class UploadTrackViewModel(MainViewModel mainShell) : ViewModelBase
{
    private readonly ApiService _api;
    private readonly IFileDialogService _fileDialogService = new AvaloniaFileDialogService();
    
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _artist = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    private async Task ExecuteBrowseAsync()
    {
        var paths = await _fileDialogService.SelectPathsAsync();
        if (paths is { Length: > 0 })
        {
            FilePath = paths[0];            
        }
    }

    [RelayCommand]
    private async Task ExecuteUploadAsync()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || string.IsNullOrWhiteSpace(Title) ||
            string.IsNullOrWhiteSpace(Artist))
        {
            StatusMessage = "All parameters (Title, Artist and File) must be set.";
            return;
        }
        
        IsBusy = true;
        StatusMessage = "Initiating file upload...";

        var request = new UploadTrackRequest(Title, Artist, FilePath);
        var response = await _api.UploadTrackAsync(request);

        StatusMessage = response.Message!;
    }

    [RelayCommand]
    private void Cancel()
    {
        var authManager = new AuthStateManager(_api);
        mainShell.SetOverlay(new ProfileViewModel(mainShell, authManager, _api));
    }
}