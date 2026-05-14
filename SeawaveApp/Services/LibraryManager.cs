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
    private readonly ApiService _api = api;
    private readonly LocalDatabaseService _db = db;
    private readonly PlaybackManager _playback = playback;
    private readonly LocalDiscoveryService _discovery = discovery;

    public ObservableCollection<Playlist> AllPlaylists { get; } = [];

    public Playlist TemporaryPlaylist { get; } = new() { Name = "Now Playing (Local)", IsOnline = false };

    public async Task RefreshPlaylistsAsync(bool includeOnline)
    {
        AllPlaylists.Clear();

        //TODO: implement get offline playlists from SQLite
        
        if (includeOnline)
        {
            var onlineResponse = await _api.GetUserPlaylistsAsync();
            if (onlineResponse is { IsSuccess: true, Data: not null })
            {
                foreach (var playlist in onlineResponse.Data)
                {
                    AllPlaylists.Add(new Playlist { Id = playlist.Id.ToString(), Name = playlist.Name, 
                        IsOnline = true });
                }
            }
        }
    }

    public async Task<ApiResult> AddTrackToPlaylist(UnifiedTrack track, Playlist playlist)
    {
        if (!playlist.CanAddTrack(track))
        {
            return new ApiResult(false, "Online playlists can only contain online tracks.");
        }

        if (playlist.IsOnline)
        {
            var response = await _api.AddTrackToPlaylistAsync(int.Parse(playlist.Id),
                int.Parse(track.Id));
            return new ApiResult(response.IsSuccess, response.Message);
        }
        else
        {
            await _db.AddTrackToPlaylistAsync(int.Parse(playlist.Id), track);
            return new ApiResult(true, "Added track to playlist.");
        }
    }

    public void PlayNow(UnifiedTrack track)
    {
        _playback.TracksQueue.Clear();
        _playback.TracksQueue.Add(track);
        _playback.PlaySingle(track);
    }

    public void PlayPlaylistNow(Playlist playlist)
    {
            _playback.PlayFromPlaylist(playlist.Tracks, startIndex: 0);
    }

    public void AddToQueue(UnifiedTrack track)
    {
        _playback.AddToQueue(track);
    }

    public async Task AddLocalFileToTemp(string[] paths)
    {
        TemporaryPlaylist.Tracks.Clear();
        foreach (var path in paths)
        {
            var tracks = await _discovery.DiscoverAsync(path);
            foreach (var track in tracks)
            {
                TemporaryPlaylist.Tracks.Add(track);
            }
        }
    }
}