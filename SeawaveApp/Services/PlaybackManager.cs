using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using SeawaveApp.Helpers;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class PlaybackManager : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;

    private readonly Random _rng = new();

    public List<UnifiedTrack> TracksQueue { get; } = [];
    public List<int> PlaybackOrder { get; private set; } = [];
    public int OrderIndex { get; private set; } = -1;
    public MediaPlayerState PlaybackState { get; private set; } = MediaPlayerState.Stopped;
    public RepeatMode CurrentRepeatMode { get; private set; } = RepeatMode.None;
    public bool IsShuffle { get; private set; }

    public UnifiedTrack? CurrentTrack => (OrderIndex >= 0 && OrderIndex < PlaybackOrder.Count)
        ? TracksQueue[PlaybackOrder[OrderIndex]]
        : null;

    public TimeSpan Position => TimeSpan.FromMilliseconds(_mediaPlayer.Time);
    public TimeSpan Duration => TimeSpan.FromMilliseconds(_mediaPlayer.Length);

    public event EventHandler<UnifiedTrack?>? TrackChanged;
    public event EventHandler? PlaybackStateChanged;
    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<TimeSpan>? DurationChanged;
    public event EventHandler<bool>? ShuffleChanged;
    public event EventHandler<RepeatMode>? RepeatChanged;
    public event EventHandler? QueueChanged;

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
        _mediaPlayer.Playing += (_, _) =>
        {
            PlaybackState = MediaPlayerState.Playing;
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        };
        _mediaPlayer.Paused += (_, _) =>
        {
            PlaybackState = MediaPlayerState.Paused;
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        };
        _mediaPlayer.Stopped += (_, _) =>
        {
            PlaybackState = MediaPlayerState.Stopped;
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public void PlayFromPlaylist(IEnumerable<UnifiedTrack> tracks, int startIndex)
    {
        TracksQueue.Clear();
        foreach (var track in tracks)
        {
            TracksQueue.Add(track);
        }

        RebuildOrder(startIndex);
        PlayCurrent();
    }

    public void PlaySingle(UnifiedTrack track)
    {
        TracksQueue.Clear();
        TracksQueue.Add(track);
        RebuildOrder(startIndex: 0);
        PlayCurrent();
    }

    public void PlayFromQueue(UnifiedTrack selectedTrack)
    {
        var trackIndex = TracksQueue.IndexOf(selectedTrack);
        OrderIndex = PlaybackOrder.IndexOf(trackIndex);

        PlayCurrent();
    }

    public void AddToQueue(UnifiedTrack track)
    {
        TracksQueue.Add(track);

        if (PlaybackOrder.Count == 0)
        {
            RebuildOrder(0);
            TrackChanged?.Invoke(this, CurrentTrack);
        }
        else
        {
            UpdateOrderForNewTrack();
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
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
            if (OrderIndex >= orderPosition)
            {
                OrderIndex--;
            }
            else if (OrderIndex == orderPosition && OrderIndex >= PlaybackOrder.Count)
            {
                OrderIndex = PlaybackOrder.Count - 1;
            }
        }

        for (var i = 0; i < PlaybackOrder.Count; i++)
        {
            if (PlaybackOrder[i] > index)
            {
                PlaybackOrder[i]--;
            }
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
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
            OrderIndex = 0;
        }
        else
        {
            OrderIndex = startIndex;
        }
    }

    private void UpdateOrderForNewTrack()
    {
        var newIndex = TracksQueue.Count - 1;
        if (IsShuffle)
        {
            var insertPosition = _rng.Next(OrderIndex + 1, PlaybackOrder.Count + 1);
            PlaybackOrder.Insert(insertPosition, newIndex);
        }
        else
        {
            PlaybackOrder.Add(newIndex);
        }
    }

    private void PlayCurrent()
    {
        if (OrderIndex < 0 || OrderIndex >= PlaybackOrder.Count)
        {
            return;
        }

        var trackIndex = PlaybackOrder[OrderIndex];
        var track = TracksQueue[trackIndex];
        var media = GetMediaForTrack(track);

        _mediaPlayer.Play(media);
        TrackChanged?.Invoke(this, CurrentTrack);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    private Media GetMediaForTrack(UnifiedTrack track)
    {
        var media = track.IsRemote
            ? new Media(_libVlc, new Uri(track.RemoteUrl!))
            : new Media(_libVlc, track.LocalPath!);

        if (track.StartOffset > TimeSpan.Zero)
        {
            media.AddOption($":start-time={track.StartOffset.TotalSeconds}");
        }

        return media;
    }

    private void HandleTrackEnd()
    {
        Dispatcher.UIThread.Post(Next);
    }

    public void TogglePause()
    {
        if (PlaybackState == MediaPlayerState.Stopped)
        {
            PlayCurrent();
        }

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
        if (CurrentRepeatMode == RepeatMode.Track)
        {
            PlayCurrent();
        }
        else if (OrderIndex < PlaybackOrder.Count - 1)
        {
            OrderIndex++;
            PlayCurrent();
        }
        else if (CurrentRepeatMode == RepeatMode.All)
        {
            OrderIndex = 0;
            PlayCurrent();
        }
        else
        {
            ClearQueue();
            _mediaPlayer.Stop();
            TrackChanged?.Invoke(this, CurrentTrack);
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Previous()
    {
        if (OrderIndex <= 0)
        {
            return;
        }

        OrderIndex--;
        PlayCurrent();
    }

    public void Seek(TimeSpan position) =>
        _mediaPlayer.Position = (float)(position.TotalMilliseconds / _mediaPlayer.Length);

    public void ToggleShuffle()
    {
        IsShuffle = !IsShuffle;

        if (OrderIndex >= 0 && OrderIndex < PlaybackOrder.Count)
        {
            RebuildOrder(PlaybackOrder[OrderIndex]);
        }

        ShuffleChanged?.Invoke(this, IsShuffle);
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

    public void ClearQueueNextUpTracks()
    {
        PlaybackOrder.KeepOnlyIndex(OrderIndex);
        OrderIndex = 0;
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearQueue()
    {
        TracksQueue.Clear();
        PlaybackOrder.Clear();
        OrderIndex = -1;
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
        GC.SuppressFinalize(this);
    }
}