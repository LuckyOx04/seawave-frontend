using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class LeftBarViewModel : ViewModelBase
{
    private readonly LibraryManager _libraryManager;
    private readonly MainViewModel _mainShell;

    private bool _isOnlineTarget;

    [ObservableProperty] public partial string PlaylistSearchQuery { get; set; } = string.Empty;
    [ObservableProperty] public partial bool ShowOnlineOnly { get; set; }
    [ObservableProperty] public partial bool ShowOfflineOnly { get; set; }
    [ObservableProperty] public partial Playlist? SelectedPlaylist { get; set; }
    [ObservableProperty] public partial bool IsPlaylistWizardActive { get; set; }
    [ObservableProperty] public partial string WizardTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string NewPlaylistName { get; set; } = string.Empty;
    [ObservableProperty] public partial string? WizardMessage { get; set; }
    [ObservableProperty] public partial bool IsFlyoutVisible { get; set; }

    public ObservableCollection<Playlist> DisplayedPlaylists { get; } = [];

    public LeftBarViewModel(MainViewModel mainShell, LibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
        _mainShell = mainShell;

        _libraryManager.AllPlaylists.CollectionChanged += OnSourcePlaylistsChanged;

        RefreshDisplayedPlaylists();
    }

    partial void OnPlaylistSearchQueryChanged(string value)
    {
        _ = value;
        RefreshDisplayedPlaylists();
    }

    partial void OnShowOnlineOnlyChanged(bool value)
    {
        if (value)
        {
            ShowOfflineOnly = false;
        }

        RefreshDisplayedPlaylists();
    }

    partial void OnShowOfflineOnlyChanged(bool value)
    {
        if (value)
        {
            ShowOnlineOnly = false;
        }

        RefreshDisplayedPlaylists();
    }

    partial void OnSelectedPlaylistChanged(Playlist? value)
    {
        _ = HandlePlaylistSelectionAsync(value);
    }

    private void OnSourcePlaylistsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshDisplayedPlaylists();
    }

    private void RefreshDisplayedPlaylists()
    {
        DisplayedPlaylists.Clear();

        var query = _libraryManager.AllPlaylists.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(PlaylistSearchQuery))
        {
            query = query.Where(p => p.Name.Contains(PlaylistSearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (ShowOnlineOnly)
        {
            query = query.Where(p => p.IsOnline);
        }
        else if (ShowOfflineOnly)
        {
            query = query.Where(p => !p.IsOnline);
        }

        foreach (var playlist in query)
        {
            DisplayedPlaylists.Add(playlist);
        }
    }

    private async Task HandlePlaylistSelectionAsync(Playlist? playlist)
    {
        if (playlist == null)
        {
            return;
        }

        if (playlist.Tracks.Count == 0)
        {
            await _libraryManager.LoadPlaylistTracksAsync(playlist);
        }

        _mainShell.NavigateToPlaylist(playlist);
    }

    [RelayCommand]
    private void OpenFlyout()
    {
        IsFlyoutVisible = true;
    }

    [RelayCommand]
    private void SelectPlaylistType(bool isOnlineSelected)
    {
        _isOnlineTarget = isOnlineSelected;

        WizardTitle = _isOnlineTarget ? "New Online Playlist" : "New Local Playlist";

        NewPlaylistName = string.Empty;

        IsPlaylistWizardActive = true;

        WizardMessage = null;
    }

    [RelayCommand]
    private async Task ConfirmWizard()
    {
        if (string.IsNullOrWhiteSpace(NewPlaylistName))
        {
            WizardMessage = "Playlist name cannot be empty.";
            return;
        }

        WizardMessage = null;

        await _libraryManager.CreatePlaylistAsync(NewPlaylistName, _isOnlineTarget);

        RefreshDisplayedPlaylists();

        ResetWizard();
        
        IsFlyoutVisible = false;

        await _mainShell.RefreshDisplayedPlaylists();
    }

    [RelayCommand]
    private void ResetWizard()
    {
        IsPlaylistWizardActive = false;
        NewPlaylistName = string.Empty;
        WizardTitle = string.Empty;
    }
}