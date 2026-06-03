using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class LibraryManager(
    ApiService api,
    LocalDatabaseService db,
    PlaybackManager playback,
    LocalDiscoveryService discovery)
{

    public ObservableCollection<Playlist> AllPlaylists { get; } = [];

    public Playlist TemporaryPlaylist { get; } = new() {Id = "0", Name = "Now Playing (Local)", IsOnline = false };

    public async Task RefreshPlaylistsAsync(bool includeOnline)
    {
        AllPlaylists.Clear();

        var localPlaylists = await db.GetAllPlaylistsAsync();
        foreach (var localPlaylist in localPlaylists)
        {
            AllPlaylists.Add(localPlaylist);
        }
        
        if (includeOnline)
        {
            var onlineResponse = await api.GetUserPlaylistsAsync();
            if (onlineResponse is { IsSuccess: true, Data: not null })
            {
                foreach (var playlist in onlineResponse.Data)
                {
                    AllPlaylists.Add(new Playlist { 
                        Id = playlist.Id.ToString(), 
                        Name = playlist.Name, 
                        IsOnline = true 
                    });
                }
            }
        }
    }

    public async Task LoadPlaylistTracksAsync(Playlist playlist)
    {
        playlist.Tracks.Clear();

        if (playlist.IsOnline)
        {
            var response = await api.GetPlaylistDetailsAsync(int.Parse(playlist.Id));
            if (response is { IsSuccess: true, Data: not null })
            {
                foreach (var trackData in response.Data.Tracks)
                {
                    playlist.Tracks.Add(new UnifiedTrack
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
        }
        else
        {
            var localTracks = await db.GetPlaylistTracksAsync(playlist.Id);
            foreach (var track in localTracks)
            {
                playlist.Tracks.Add(track);
            }
        }
    }

    public async Task CreatePlaylist(string playlistName, bool isOnlinePlaylist)
    {
        if (isOnlinePlaylist)
        {
            await api.CreatePlaylistAsync(new CreatePlaylistRequest(playlistName));
        }
        else
        {
            var uniquePlaylistSeed = $"{playlistName}_{Guid.NewGuid()}";
            var playlistId = IdGenerator.GenerateSha256Id(uniquePlaylistSeed);
            await db.CretePlaylistAsync(playlistId, playlistName);
        }
    }

    public void PlayTrackFromPlaylist(Playlist playlist, UnifiedTrack track)
    {
        var index = playlist.Tracks.IndexOf(track);
        if (index >= 0)
        {
            playback.PlayFromPlaylist(playlist.Tracks, index);
        }
        else
        {
            playback.PlaySingle(track);
        }
    }

    public void PlayTrackFromSearch(UnifiedTrack track)
    {
        playback.PlaySingle(track);
    }

    public async Task PlayPlaylistAsync(Playlist playlist)
    {
        switch (playlist.Tracks.Count)
        {
            case 0:
                await LoadPlaylistTracksAsync(playlist);
                break;
            case > 0:
                playback.PlayFromPlaylist(playlist.Tracks, 0);
                break;
        }
    }
    
    public void AddTrackToQueue(UnifiedTrack track)
    {
        playback.AddToQueue(track);
    }

    public async Task AddPlaylistToQueueAsync(Playlist playlist)
    {
        if (playlist.Tracks.Count == 0)
        {
            await LoadPlaylistTracksAsync(playlist);
        }

        foreach (var track in playlist.Tracks)
        {
            playback.AddToQueue(track);
        }
    }
    
    public async Task<ApiResult> AddTrackToPlaylistAsync(UnifiedTrack track, Playlist playlist)
    {
        if (!playlist.CanAddTrack(track))
        {
            return new ApiResult(false, "Online playlists can only contain online tracks.");
        }

        if (playlist.IsOnline)
        {
            var response = await api.AddTrackToPlaylistAsync(int.Parse(playlist.Id),
                int.Parse(track.Id));
            return new ApiResult(response.IsSuccess, response.Message);
        }

        await db.AddTrackToPlaylistAsync(playlist.Id, track);
        return new ApiResult(true, "Added track to playlist.");
    }

    public async Task AddLocalFileToTempAsync(string[] paths)
    {
        foreach (var path in paths)
        {
            var tracks = await discovery.DiscoverAsync(path);
            foreach (var track in tracks)
            {
                TemporaryPlaylist.Tracks.Add(track);
            }
        }
    }

    public async Task ClearTemporaryPlaylistAsync()
    {
        await Task.Run(() => TemporaryPlaylist.Tracks.Clear());
    }
}