using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class PlaybackManager : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;

    public ObservableCollection<UnifiedTrack> TracksQueue { get; } = new();
    public int CurrentTrackIndex { get; private set; } = -1;

    public PlaybackManager()
    {
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        
        _mediaPlayer.EndReached += (s, e) => Task.Run(Next);
    }

    public void Play(UnifiedTrack track)
    {
        Media? media = null;

        if (track.IsRemote)
        {
            media = new Media(_libVlc, new Uri(track.RemoteUrl!));
        }
        else
        {
            media = new Media(_libVlc, track.LocalPath!, FromType.FromPath);
        }

        if (track.StartOffset > TimeSpan.Zero)
        {
            media.AddOption($":start-time={track.StartOffset.TotalSeconds}");
        }
        
        _mediaPlayer.Play(media);
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
        if (CurrentTrackIndex >= TracksQueue.Count - 1)
        {
            return;
        }
        CurrentTrackIndex++;
        Play(TracksQueue[CurrentTrackIndex]);
    }

    public void Previous()
    {
        if (CurrentTrackIndex <= 0)
        {
            return;
        }
        CurrentTrackIndex--;
        Play(TracksQueue[CurrentTrackIndex]);
    }

    public void Seek(TimeSpan position) =>
        _mediaPlayer.Position = (float)(position.TotalMilliseconds / _mediaPlayer.Length);

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
    }
}