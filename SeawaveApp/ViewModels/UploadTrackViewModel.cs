using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class UploadTrackViewModel(MainViewModel mainShell, AuthStateManager authStateManager,
    ApiService api) : ViewModelBase
{
    private readonly AvaloniaFileDialogService _fileDialogService = new();
    
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#007bff"));
    private static readonly IBrush FailureBrush = new SolidColorBrush(Color.Parse("#ff6666"));
    
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _artist = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(StatusColor))]
    private bool _isSuccessState;
    
    public IBrush StatusColor => IsSuccessState ? SuccessBrush : FailureBrush;

    [RelayCommand]
    private async Task ExecuteBrowseAsync()
    {
        var paths = await _fileDialogService.SelectPathsAsync(true);
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
            IsSuccessState = false;
            return;
        }
        
        IsBusy = true;
        IsSuccessState = true;
        StatusMessage = "Initiating file upload...";

        var request = new UploadTrackRequest(Title, Artist, FilePath);
        var response = await api.UploadTrackAsync(request);

        IsBusy = false;

        if (response.IsSuccess)
        {
            Title = string.Empty;
            Artist = string.Empty;
            FilePath = string.Empty;
            IsSuccessState = true;
            StatusMessage = "Track successfully uploaded for review.";
        }
        else
        {
            IsSuccessState = false;
            StatusMessage = response.Message!;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        mainShell.ActiveOverlay = new ProfileViewModel(mainShell, authStateManager, api);
    }
}