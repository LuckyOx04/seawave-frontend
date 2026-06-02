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

    public ObservableCollection<Playlist> FilteredPlaylists { get; } = [];

    public LeftBarViewModel(MainViewModel mainShell, LibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
        _mainShell = mainShell;

        _libraryManager.AllPlaylists.CollectionChanged += OnSourcePlaylistsChanged;

        ApplyFilter();
    }

    partial void OnPlaylistSearchQueryChanged(string value)
    {
        _ = value;
        ApplyFilter();
    }

    partial void OnShowOnlineOnlyChanged(bool value)
    {
        if (value)
        {
            ShowOfflineOnly = false;
        }

        ApplyFilter();
    }

    partial void OnShowOfflineOnlyChanged(bool value)
    {
        if (value)
        {
            ShowOnlineOnly = false;
        }

        ApplyFilter();
    }

    partial void OnSelectedPlaylistChanged(Playlist? value)
    {
        _ = HandlePlaylistSelectionAsync(value);
    }

    private void OnSourcePlaylistsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredPlaylists.Clear();

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
            FilteredPlaylists.Add(playlist);
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

        await _mainShell.NavigateToPlaylist(playlist);
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
    private void ConfirmWizard()
    {
        if (string.IsNullOrWhiteSpace(NewPlaylistName))
        {
            WizardMessage = "Playlist name cannot be empty.";
            return;
        }

        WizardMessage = null;

        Console.WriteLine(
            $"Flyout Extracted Data -> Title: {NewPlaylistName}, IsOnline: {_isOnlineTarget}");

        ResetWizard();
    }

    [RelayCommand]
    private void ResetWizard()
    {
        IsPlaylistWizardActive = false;
        NewPlaylistName = string.Empty;
        WizardTitle = string.Empty;
    }
}