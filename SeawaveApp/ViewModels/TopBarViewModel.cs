using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Helpers;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class TopBarViewModel(
    ApiService api,
    LibraryManager libraryManager,
    MainViewModel mainShell,
    IFileDialog fileDialog)
    : ViewModelBase
{
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested && !string.IsNullOrWhiteSpace(value))
                {
                    await PerformSearchAsync(value);
                }
            }
            catch (TaskCanceledException)
            {
                
            }
        }, token);
    }

    [RelayCommand]
    private async Task ExecuteSearchAsync()
    {
        await _searchCts?.CancelAsync()!;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            await PerformSearchAsync(SearchQuery);
        }
    }

    private async Task PerformSearchAsync(string query)
    {
        mainShell.CurrentCenterMode = CenterContentMode.SearchResults;
        mainShell.TracksSearchResults.Clear();
        mainShell.PlaylistsSearchResults.Clear();

        var trackSearchResponse = await api.SearchTracksAsync(query);
        var playlistSearchResponse = await api.SearchPlaylistsAsync(query);
        
        if (trackSearchResponse is { IsSuccess: true, Data: not null })
        {
            foreach (var trackData in trackSearchResponse.Data)
            {
                mainShell.TracksSearchResults.Add(new UnifiedTrack
                {
                    Id = trackData.Id.ToString(),
                    Title = trackData.Title,
                    Artist = trackData.Artist,
                    Album = null,
                    Duration = TimeSpan.FromSeconds(trackData.DurationSeconds),
                    IsRemote = true,
                    RemoteUrl = api.GetStreamUrl(trackData.FileName)
                });
            }
        }

        if (playlistSearchResponse is { IsSuccess: true, Data: not null })
        {
            foreach (var playlistData in playlistSearchResponse.Data)
            {
                mainShell.PlaylistsSearchResults.Add(new Playlist
                {
                    Id = playlistData.Id.ToString(),
                    Name = playlistData.Name,
                    IsOnline = true,
                });
            }
        }
    }

    [RelayCommand]
    private async Task AddFileAsync()
    {
        var paths = await fileDialog.SelectPathsAsync(false);
        if (paths is { Length: > 0 })
        {
            await libraryManager.AddLocalFileToTempAsync(paths);
            await libraryManager.RefreshPlaylistsAsync(mainShell is { IsOnline: true, IsLoggedIn: true });
            mainShell.NavigateToPlaylist(libraryManager.TemporaryPlaylist);
        }
    }
}