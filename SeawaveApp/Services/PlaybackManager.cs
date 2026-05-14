using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class PlaybackManager : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    
    private List<int> _playbackOrder = [];
    private int _orderIndex = -1;
    private Random _rng = new();

    public ObservableCollection<UnifiedTrack> TracksQueue { get; } = [];
    
    public RepeatMode CurrentRepeatMode { get; set; } = RepeatMode.None;
    public bool IsShuffle { get; set; }

    public PlaybackManager()
    {
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        _mediaPlayer.EndReached += (s, e) => HandleTrackEnd();
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
        _playbackOrder = Enumerable.Range(0, TracksQueue.Count).ToList();

        if (IsShuffle && _playbackOrder.Count > 1)
        {
            var current = _playbackOrder[startIndex];
            _playbackOrder.RemoveAt(startIndex);
            _playbackOrder = _playbackOrder.OrderBy(x => _rng.Next()).ToList();
            _playbackOrder.Insert(0, current);
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
            var insertPosition = _rng.Next(_orderIndex + 1, _playbackOrder.Count + 1);
            _playbackOrder.Insert(insertPosition, newIndex);
        }
        else
        {
            _playbackOrder.Add(newIndex);
        }
    }

    private void PlayCurrent()
    {
        if (_orderIndex < 0 || _orderIndex >= _playbackOrder.Count)
        {
            return;
        }
        
        var trackIndex = _playbackOrder[_orderIndex];
        var track = TracksQueue[trackIndex];
        var media = GetMediaForTrack(track);

        _mediaPlayer.Play(media);
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
        if (_orderIndex < _playbackOrder.Count - 1)
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

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
        GC.SuppressFinalize(this);
    }
}