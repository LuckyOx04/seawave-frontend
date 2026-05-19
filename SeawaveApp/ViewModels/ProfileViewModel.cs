using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly MainViewModel _mainShell;
    private readonly AuthStateManager _authStateManager;
    private readonly ApiService _api;

    [ObservableProperty] private string _usernameText = "Loading...";
    [ObservableProperty] private string _emailText = "Loading...";
    [ObservableProperty] private string _createdAt = "Loading...";
    [ObservableProperty] private int _createdPlaylistsCount;
    [ObservableProperty] private int _pendingTracksCount;
    [ObservableProperty] private int _approvedTracksCount;

    public ProfileViewModel(MainViewModel mainShell, AuthStateManager authStateManager, ApiService api)
    {
        _mainShell = mainShell;
        _authStateManager = authStateManager;
        _api = api;

        _ = LoadUserProfileDataAsync();
    }

    private async Task LoadUserProfileDataAsync()
    {
        var response = await _api.GetUserProfileInfoAsync();
        if (response is { IsSuccess: true, Data: not null })
        {
            UsernameText = response.Data.Username;
            EmailText = response.Data.Email;
            CreatedAt = response.Data.CreatedAt.ToString("dd MMM yyyy", new CultureInfo("en-US"));
            PendingTracksCount = response.Data.PendingTracksCount;
            ApprovedTracksCount = response.Data.ApprovedTracksCount;
        }
        else
        {
            UsernameText = _authStateManager.Username ?? "Guest";
            EmailText = string.Empty;
        }
    }

    [RelayCommand]
    private void NavigateToUploadTrack()
    {
        _mainShell.ActiveOverlay = new UploadTrackViewModel(_mainShell, _authStateManager, _api);
    }

    [RelayCommand]
    private void NavigateToChangePassword()
    {
        _mainShell.ActiveOverlay = new ChangePasswordViewModel(_mainShell, _authStateManager, _api);
    }

    [RelayCommand]
    private async Task ExecuteLogoutAsync()
    {
        await _authStateManager.Logout();
        _mainShell.ActiveOverlay = null;
    }

    [RelayCommand]
    private void ReturnToMainWindow()
    {
        _mainShell.ActiveOverlay = null;
    }
}