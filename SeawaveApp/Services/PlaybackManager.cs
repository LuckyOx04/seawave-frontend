using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class PlaybackManager : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    
    private int _orderIndex = -1;
    private readonly Random _rng = new();

    public List<UnifiedTrack> TracksQueue { get; } = [];
    public List<int> PlaybackOrder { get; private set; } = [];
    
    public RepeatMode CurrentRepeatMode { get; private set; } = RepeatMode.None;
    public bool IsShuffle { get; private set; }

    public UnifiedTrack? CurrentTrack => (_orderIndex >= 0 && _orderIndex < PlaybackOrder.Count)
        ? TracksQueue[PlaybackOrder[_orderIndex]]
        : null;

    public bool IsPlaying => _mediaPlayer.IsPlaying;
    public TimeSpan Position => TimeSpan.FromMilliseconds(_mediaPlayer.Time);
    public TimeSpan Duration => TimeSpan.FromMilliseconds(_mediaPlayer.Length);

    public event EventHandler<UnifiedTrack?>? TrackChanged;
    public event EventHandler<bool>? PlaybackStateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<TimeSpan>? DurationChanged;
    public event EventHandler<bool>? ShuffleChanged;
    public event EventHandler<RepeatMode>? RepeatChanged;

    public PlaybackManager()
    {
        Core.Initialize();
        
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);

        _mediaPlayer.EndReached += (_, _) => HandleTrackEnd();
        _mediaPlayer.TimeChanged += (_, e) => PositionChanged?
            .Invoke(this, TimeSpan.FromMilliseconds(e.Time));
        _mediaPlayer.LengthChanged += (_, e) => DurationChanged?
            .Invoke(this, TimeSpan.FromMilliseconds(e.Length));
        _mediaPlayer.Playing += (_, _) => PlaybackStateChanged?.Invoke(this, true);
        _mediaPlayer.Paused += (_, _) => PlaybackStateChanged?.Invoke(this, false);
        _mediaPlayer.Stopped += (_, _) => PlaybackStateChanged?.Invoke(this, false);
    }

    public void PlayFromPlaylist(IEnumerable<UnifiedTrack> tracks, int startIndex)
    {
        TracksQueue.Clear();
        foreach (var track in tracks.Skip(startIndex))
        {
            TracksQueue.Add(track);
        }

        RebuildOrder(startIndex: 0);
    }

    public void PlaySingle(UnifiedTrack track)
    {
        TracksQueue.Clear();
        TracksQueue.Add(track);
        RebuildOrder(startIndex: 0);
    }

    public void AddToQueue(UnifiedTrack track)
    {
        TracksQueue.Add(track);
        UpdateOrderForNewTrack();
    }

    private void RebuildOrder(int startIndex)
    {
        PlaybackOrder = Enumerable.Range(0, TracksQueue.Count).ToList();

        if (IsShuffle && PlaybackOrder.Count > 1)
        {
            var current = PlaybackOrder[startIndex];
            PlaybackOrder.RemoveAt(startIndex);
            PlaybackOrder = PlaybackOrder.OrderBy(_ => _rng.Next()).ToList();
            PlaybackOrder.Insert(0, current);
            _orderIndex = 0;
        }
        else
        {
            _orderIndex = startIndex;
        }

        PlayCurrent();
    }

    private void UpdateOrderForNewTrack()
    {
        var newIndex = TracksQueue.Count - 1;
        if (IsShuffle)
        {
            var insertPosition = _rng.Next(_orderIndex + 1, PlaybackOrder.Count + 1);
            PlaybackOrder.Insert(insertPosition, newIndex);
        }
        else
        {
            PlaybackOrder.Add(newIndex);
        }
    }

    private void PlayCurrent()
    {
        if (_orderIndex < 0 || _orderIndex >= PlaybackOrder.Count)
        {
            return;
        }
        
        var trackIndex = PlaybackOrder[_orderIndex];
        var track = TracksQueue[trackIndex];
        var media = GetMediaForTrack(track);

        _mediaPlayer.Play(media);
        TrackChanged?.Invoke(this, CurrentTrack);
    }

    private Media GetMediaForTrack(UnifiedTrack track)
    {
        var media = track.IsRemote ? new Media(_libVlc, new Uri(track.RemoteUrl!)) 
            : new Media(_libVlc, track.LocalPath!);

        if (track.StartOffset > TimeSpan.Zero)
        {
            media.AddOption($":start-time={track.StartOffset.TotalSeconds}");
        }

        return media;
    }

    private void HandleTrackEnd()
    {
        Task.Run(() =>
        {
            if (CurrentRepeatMode == RepeatMode.Track)
            {
                PlayCurrent();
                return;
            }
            
            Next();
        });
    }

    public void TogglePause()
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            _mediaPlayer.Play();
        }
    }

    public void Next()
    {
        if (_orderIndex < PlaybackOrder.Count - 1)
        {
            _orderIndex++;
            PlayCurrent();
        }
        else if (CurrentRepeatMode == RepeatMode.All)
        {
            _orderIndex = 0;
            PlayCurrent();
        }
    }

    public void Previous()
    {
        if (_orderIndex <= 0)
        {
            return;
        }
        _orderIndex--;
        PlayCurrent();
    }

    public void Seek(TimeSpan position) =>
        _mediaPlayer.Position = (float)(position.TotalMilliseconds / _mediaPlayer.Length);

    public void ToggleShuffle()
    {
        IsShuffle = !IsShuffle;
        ShuffleChanged?.Invoke(this, IsShuffle);

        if (_orderIndex >= 0 && _orderIndex < PlaybackOrder.Count)
        {
            RebuildOrder(_orderIndex);
        }
    }

    public void CycleRepeat()
    {
        CurrentRepeatMode = CurrentRepeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.Track,
            _ => RepeatMode.None
        };
        RepeatChanged?.Invoke(this, CurrentRepeatMode);
    }

    public void RemoveTrack(UnifiedTrack track)
    {
        var index = TracksQueue.IndexOf(track);
        if (index < 0)
        {
            return;
        }
        
        TracksQueue.RemoveAt(index);

        var orderPosition = PlaybackOrder.IndexOf(index);
        if (orderPosition >= 0)
        {
            PlaybackOrder.RemoveAt(orderPosition);
            if (_orderIndex >= orderPosition) // TODO: check for when the removed index is the current playing.
            {
                _orderIndex--;
            }
            else if (_orderIndex == orderPosition && _orderIndex >= PlaybackOrder.Count)
            {
                _orderIndex = PlaybackOrder.Count - 1;
            }
        }

        for (var i = 0; i < PlaybackOrder.Count; i++)
        {
            if (PlaybackOrder[i] > index)
            {
                PlaybackOrder[i]--;
            }
        }
    }

    public void ClearQueue()
    {
        TracksQueue.Clear();
        PlaybackOrder.Clear();
        _orderIndex = -1;
    }
    
    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
        GC.SuppressFinalize(this);
    }
}