using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class CenterAreaViewModel : ViewModelBase
{
    private readonly MainViewModel _mainShell;
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty]
    public partial string HeaderTitle { get; set; } = "Select a Playlist, Search for Music or Add Local Files";

    [ObservableProperty] public partial bool IsClearable { get; set; }
    public ObservableCollection<UnifiedTrack> DisplayTracks { get; } = [];


    public CenterAreaViewModel(MainViewModel mainShell, PlaybackManager playbackManager)
    {
        _mainShell = mainShell;
        _playbackManager = playbackManager;

        _mainShell.PropertyChanged += OnShellPropertyChanged;

        _mainShell.SearchResults.CollectionChanged += OnSearchResultsChanged;

        IsClearable = _mainShell.ActiveCenterPlaylist?.Id == "0";

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

                IsClearable = _mainShell.ActiveCenterPlaylist?.Id == "0";
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

                    IsClearable = _mainShell.ActiveCenterPlaylist.Id == "0";
                }

                break;
            case CenterContentMode.None:
            default:
                HeaderTitle = "Select a Playlist, Search for Music or Add Local Files";
                IsClearable = _mainShell.ActiveCenterPlaylist?.Id == "0";
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

    [RelayCommand]
    private async Task ClearPlaylistAsync()
    {
        await _mainShell.NavigateToPlaylist(null);
    }
}