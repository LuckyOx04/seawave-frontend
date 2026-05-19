using System;
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
    [ObservableProperty] private DateTime _createdAt;
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
            CreatedAt = response.Data.CreatedAt;
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
        _mainShell.SetOverlay(new UploadTrackViewModel(_mainShell));
    }

    [RelayCommand]
    private void NavigateToChangePassword()
    {
        _mainShell.SetOverlay(new ChangePasswordViewModel(_mainShell));
    }

    [RelayCommand]
    private async Task ExecuteLogoutAsync()
    {
        await _authStateManager.Logout();
        _mainShell.SetOverlay(new LoginViewModel(_mainShell, _authStateManager));
    }

    [RelayCommand]
    private void ReturnToMainWindow()
    {
        _mainShell.SetOverlay(null);
    }
}