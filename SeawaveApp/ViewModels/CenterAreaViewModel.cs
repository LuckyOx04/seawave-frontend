using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class CenterAreaViewModel : ViewModelBase
{
    private readonly MainViewModel _mainShell;
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty] private string _headerTitle = "Welcome to Seawave";

    public ObservableCollection<UnifiedTrack> DisplayTracks { get; } = [];

    public CenterAreaViewModel(MainViewModel mainShell, PlaybackManager playbackManager)
    {
        _mainShell = mainShell;
        _playbackManager = playbackManager;

        _mainShell.PropertyChanged += OnShellPropertyChanged;

        _mainShell.SearchResults.CollectionChanged += OnSearchResultsChanged;
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.CurrentCenterMode) or nameof(MainViewModel.ActiveCenterPlaylist))
        {
            SyncDisplayTracks();
        }
    }

    private void OnSearchResultsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_mainShell.CurrentCenterMode == CenterContentMode.SearchResults)
        {
            SyncDisplayTracks();
        }
    }

    private void SyncDisplayTracks()
    {
        DisplayTracks.Clear();

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
            case CenterContentMode.TemporaryTracks:
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
                HeaderTitle = "Select a Playlist or Search for Music";
                break;
        }
    }

    [RelayCommand]
    private void PlayTrack(UnifiedTrack? selectedTrack)
    {
        if (selectedTrack == null)
        {
            return;
        }

        var index = DisplayTracks.IndexOf(selectedTrack);
        if (index >= 0)
        {
            _playbackManager.PlayFromPlaylist(DisplayTracks, index);
        }
    }
}