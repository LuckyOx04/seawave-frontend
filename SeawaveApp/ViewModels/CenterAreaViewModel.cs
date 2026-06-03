using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
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

    [ObservableProperty]
    public partial string HeaderTitle { get; set; } = "Select a Playlist, Search for Music or Add Local Files";

    [ObservableProperty] public partial bool IsClearable { get; set; }
    [ObservableProperty] public partial bool IsPlaylist { get; set; }
    [ObservableProperty] public partial bool IsPlaylistFlyoutVisible { get; set; }
    public ObservableCollection<UnifiedTrack> DisplayTracks { get; } = [];


    public CenterAreaViewModel(MainViewModel mainShell, LibraryManager libraryManager)
    {
        _mainShell = mainShell;
        _libraryManager = libraryManager;

        _mainShell.PropertyChanged += OnShellPropertyChanged;

        _mainShell.SearchResults.CollectionChanged += OnSearchResultsChanged;

        IsClearable = _mainShell.ActiveCenterPlaylist?.Id == "0";
        IsPlaylist = _mainShell.CurrentCenterMode == CenterContentMode.PlaylistTracks;

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
                IsPlaylist = _mainShell.CurrentCenterMode == CenterContentMode.PlaylistTracks;
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
                    IsPlaylist = _mainShell.CurrentCenterMode == CenterContentMode.PlaylistTracks;
                }

                break;
            case CenterContentMode.None:
            default:
                HeaderTitle = "Select a Playlist, Search for Music or Add Local Files";
                IsClearable = _mainShell.ActiveCenterPlaylist?.Id == "0";
                IsPlaylist = _mainShell.CurrentCenterMode == CenterContentMode.PlaylistTracks;
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
    private void OpenFlyout()
    {
        IsPlaylistFlyoutVisible = true;
    }
    
    [RelayCommand]
    private async Task AddPlaylistToQueueAsync()
    {
        if (_mainShell.ActiveCenterPlaylist == null)
        {
            return;
        }
        
        await _libraryManager.AddPlaylistToQueueAsync(_mainShell.ActiveCenterPlaylist);
        IsPlaylistFlyoutVisible = false;
    }
    
    [RelayCommand]
    private async Task RemovePlaylistAsync()
    {
        if (_mainShell.ActiveCenterPlaylist == null)
        {
            return;
        }
        
        await _libraryManager.DeletePlaylistAsync(_mainShell.ActiveCenterPlaylist);
        IsPlaylistFlyoutVisible = false;
    }

    [RelayCommand]
    private void ClearPlaylist()
    {
        _mainShell.NavigateToPlaylist(null);
    }
}