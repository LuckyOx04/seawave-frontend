using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class LeftBarViewModel : ViewModelBase
{
    private readonly LibraryManager _libraryManager;
    private readonly MainViewModel _mainShell;

    [ObservableProperty] private string _playlistSearchQuery = string.Empty;
    [ObservableProperty] private bool _showOnlineOnly;
    [ObservableProperty] private bool _showOfflineOnly;
    [ObservableProperty] private Playlist? _selectedPlaylist;

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

    public void ApplyFilter()
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
}