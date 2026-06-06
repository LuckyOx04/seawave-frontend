using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SeawaveApp.Messages;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class CenterAreaViewModel : ViewModelBase
{
    private readonly MainViewModel _mainShell;
    private readonly LibraryManager _libraryManager;
    
    private UnifiedTrack? _selectedTrackToAddToPlaylist;

    [ObservableProperty]
    public partial string HeaderTitle { get; set; } = "Select a Playlist, Search for Music or Add Local Files";

    [ObservableProperty] public partial bool IsClearable { get; set; }
    [ObservableProperty] public partial bool IsPlaylist { get; set; }
    [ObservableProperty] public partial bool IsPlaylistSelectionFlyoutVisible { get; set; }
    
    public ObservableCollection<Playlist> FlyoutDisplayPlaylists { get; } = [];
    public ObservableCollection<UnifiedTrack> DisplayTracks { get; } = [];


    public CenterAreaViewModel(MainViewModel mainShell, LibraryManager libraryManager)
    {
        _mainShell = mainShell;
        _libraryManager = libraryManager;

        _mainShell.PropertyChanged += OnShellPropertyChanged;

        _mainShell.SearchResults.CollectionChanged += OnSearchResultsChanged;

        _libraryManager.AllPlaylists.CollectionChanged += (_, _) => RefreshFlyoutPlaylists();

        IsClearable = _mainShell.ActiveCenterPlaylist?.Id == "0";
        IsPlaylist = _mainShell.CurrentCenterMode == CenterContentMode.PlaylistTracks;
        IsPlaylistSelectionFlyoutVisible = false;

        RefreshFlyoutPlaylists();

        WeakReferenceMessenger.Default.Register<PlaylistChangedMessage>(this, (_, _) =>
        {
            SyncDisplayTracks();
        });
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.CurrentCenterMode) or nameof(MainViewModel.ActiveCenterPlaylist)
            or nameof(MainViewModel.ActiveCenterPlaylist.Tracks))
        {
            SyncDisplayTracks();
            RefreshFlyoutPlaylists();
        }
    }

    private void OnSearchResultsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_mainShell.CurrentCenterMode == CenterContentMode.SearchResults)
        {
            SyncDisplayTracks();
        }
    }

    private void RefreshFlyoutPlaylists()
    {
        FlyoutDisplayPlaylists.Clear();

        var filteredPlaylists =
            _libraryManager.AllPlaylists.Where(playlist => playlist != _mainShell.ActiveCenterPlaylist);

        if (_mainShell.ActiveCenterPlaylist is { IsOnline: false })
        {
            filteredPlaylists = filteredPlaylists.Where(playlist => !playlist.IsOnline);
        }

        foreach (var playlist in filteredPlaylists)
        {
            FlyoutDisplayPlaylists.Add(playlist);
        }
    }

    private void SyncDisplayTracks()
    {
        DisplayTracks.Clear();

        IsClearable = _mainShell.ActiveCenterPlaylist?.Id == "0";
        IsPlaylist = _mainShell.CurrentCenterMode == CenterContentMode.PlaylistTracks;

        switch (_mainShell.CurrentCenterMode)
        {
            case CenterContentMode.SearchResults:
                HeaderTitle = "Search Results";
                foreach (var track in _mainShell.SearchResults)
                {
                    DisplayTracks.Add(track);
                }

                break;
            case CenterContentMode.PlaylistTracks:
                if (_mainShell.ActiveCenterPlaylist != null)
                {
                    HeaderTitle = _mainShell.ActiveCenterPlaylist.Name;
                    foreach (var track in _mainShell.ActiveCenterPlaylist.Tracks)
                    {
                        DisplayTracks.Add(track);
                    }
                }

                break;
            case CenterContentMode.None:
            default:
                HeaderTitle = "Select a Playlist, Search for Music or Add Local Files";
                break;
        }
    }

    [RelayCommand]
    private void PlayTrack(UnifiedTrack? selectedTrack)
    {
        if (selectedTrack == null || _mainShell.ActiveCenterPlaylist == null)
        {
            return;
        }

        var index = DisplayTracks.IndexOf(selectedTrack);
        if (index >= 0)
        {
            _libraryManager.PlayTrackFromPlaylist(_mainShell.ActiveCenterPlaylist, selectedTrack);
        }
    }

    [RelayCommand]
    private async Task PlayPlaylist()
    {
        if (DisplayTracks.Count == 0 || _mainShell.ActiveCenterPlaylist == null)
        {
            return;
        }

        await _libraryManager.PlayPlaylistAsync(_mainShell.ActiveCenterPlaylist);
    }

    [RelayCommand]
    private void AddTrackToQueue(object? parameter)
    {
        if (parameter is not FlyoutPresenter { DataContext: UnifiedTrack track } presenter)
        {
            return;
        }
        
        _libraryManager.AddTrackToQueue(track);

        if (presenter.Parent is Popup popup)
        {
            popup.IsOpen = false;
        }
    }

    [RelayCommand]
    private void PickTrackToAddToPlaylist(UnifiedTrack track)
    {
        _selectedTrackToAddToPlaylist = track;
        IsPlaylistSelectionFlyoutVisible = true;
    }

    [RelayCommand]
    private async Task AddTrackToPlaylistAsync(object? parameter)
    {
        if (parameter is FlyoutPresenter presenter)
        {
            var innerListBox = presenter.FindDescendantOfType<ListBox>();

            var listBoxButton = innerListBox?.FindDescendantOfType<Button>();
            
            if (listBoxButton?.DataContext is Playlist targetPlaylist)
            {
                if (_selectedTrackToAddToPlaylist != null)
                {
                    await _libraryManager.AddTrackToPlaylistAsync(_selectedTrackToAddToPlaylist, targetPlaylist);
                }
            }

            ResetTrackFlyout();

            if (presenter.Parent is Popup popup)
            {
                popup.IsOpen = false;
            }
        }
    }
    
    [RelayCommand]
    private async Task RemoveTrackFromPlaylist(object? parameter)
    {
        if (_mainShell.ActiveCenterPlaylist == null)
        {
            return;
        }

        if (parameter is FlyoutPresenter { DataContext: UnifiedTrack track } presenter)
        {
            await _libraryManager.RemoveTrackFromPlaylistAsync(track, _mainShell.ActiveCenterPlaylist);
            SyncDisplayTracks();
            
            if (presenter.Parent is Popup popup)
            {
                popup.IsOpen = false;
            }
        }

        ResetTrackFlyout();
    }
    
    [RelayCommand]
    private void ResetTrackFlyout()
    {
        _selectedTrackToAddToPlaylist = null;
        IsPlaylistSelectionFlyoutVisible = false;
    }
    
    [RelayCommand]
    private async Task AddPlaylistToQueueAsync(object? parameter)
    {
        if (_mainShell.ActiveCenterPlaylist == null)
        {
            return;
        }

        if (parameter is FlyoutPresenter presenter)
        {
            await _libraryManager.AddPlaylistToQueueAsync(_mainShell.ActiveCenterPlaylist);

            if (presenter.Parent is Popup popup)
            {
                popup.IsOpen = false;
            }
        }
    }
    
    [RelayCommand]
    private async Task DeletePlaylistAsync(object? parameter)
    {
        if (_mainShell.ActiveCenterPlaylist == null)
        {
            return;
        }

        if (parameter is FlyoutPresenter presenter)
        {
            await _libraryManager.DeletePlaylistAsync(_mainShell.ActiveCenterPlaylist);
            await _mainShell.RefreshDisplayedPlaylists();
            _mainShell.NavigateToPlaylist(null);

            if (presenter.Parent is Popup popup)
            {
                popup.IsOpen = false;
            }
        }
    }

    [RelayCommand]
    private async Task ClearPlaylistAsync()
    {
        await _libraryManager.ClearTemporaryPlaylistAsync();
        _mainShell.ActiveCenterPlaylist?.Tracks.Clear();
        SyncDisplayTracks();
    }
}