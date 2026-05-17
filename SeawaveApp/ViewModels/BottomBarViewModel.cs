using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class BottomBarViewModel : ViewModelBase
{
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty] private UnifiedTrack? _currentTrack;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private TimeSpan _currentPosition;
    [ObservableProperty] private TimeSpan _trackDuration;
    [ObservableProperty] private bool _isShuffleOn;
    [ObservableProperty] private string _repeatModeLabel = "Repeat: Off";

    public double CurrentPositionSeconds
    {
        get => CurrentPosition.TotalSeconds;
        set
        {
            if (Math.Abs(CurrentPosition.TotalSeconds - value) > 0.9)
            {
                //SeekToSeconds(value);
            }
        }
    }

    public double TrackDurationSeconds => TrackDuration.TotalSeconds;

    public BottomBarViewModel(PlaybackManager playbackManager)
    {
        _playbackManager = playbackManager;
        
        //CurrentTrack = _playbackManager.
    }
}