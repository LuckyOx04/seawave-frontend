using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class BottomBarViewModel : ViewModelBase
{
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty] private UnifiedTrack? _currentTrack;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private double _currentPositionSeconds;
    [ObservableProperty] private double _trackDurationSeconds;
    [ObservableProperty] private bool _isShuffleOn;
    [ObservableProperty] private string _repeatModeLabel = "Repeat: Off";

    public BottomBarViewModel(PlaybackManager playbackManager)
    {
        _playbackManager = playbackManager;

        CurrentTrack = _playbackManager.CurrentTrack;
        IsPlaying = _playbackManager.IsPlaying;
        CurrentPositionSeconds = _playbackManager.Position.TotalSeconds;
        TrackDurationSeconds = _playbackManager.Duration.TotalSeconds;
        IsShuffleOn = _playbackManager.IsShuffle;
        UpdateRepeatLabel(_playbackManager.CurrentRepeatMode);

        _playbackManager.TrackChanged += (_, track) => CurrentTrack = track;
        _playbackManager.PlaybackStateChanged += (_, playing) => IsPlaying = playing;
        _playbackManager.ShuffleChanged += (_, shuffle) => IsShuffleOn = shuffle;
        _playbackManager.RepeatChanged += (_, mode) => UpdateRepeatLabel(mode);

        _playbackManager.PositionChanged += OnPositionChanged;
        _playbackManager.DurationChanged += OnDurationChanged;
    }

    private void OnPositionChanged(object? sender, TimeSpan position)
    {
        CurrentPositionSeconds = position.TotalSeconds;
    }

    private void OnDurationChanged(object? sender, TimeSpan duration)
    {
        TrackDurationSeconds = duration.TotalSeconds;
    }

    private void UpdateRepeatLabel(RepeatMode mode)
    {
        RepeatModeLabel = mode switch
        {
            RepeatMode.Track => "Repeat: One",
            RepeatMode.All => "Repeat: All",
            _ => "Repeat: Off"
        };
    }

    [RelayCommand]
    private void SeekToTime(double seconds)
    {
        CurrentPositionSeconds = seconds;
        _playbackManager.Seek(TimeSpan.FromSeconds(seconds));
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        _playbackManager.TogglePause();
    }

    [RelayCommand]
    private void SkipNext()
    {
        _playbackManager.Next();
    }

    [RelayCommand]
    private void SkipPrevious()
    {
        _playbackManager.Previous();
    }

    [RelayCommand]
    private void ToggleShuffle()
    {
        _playbackManager.ToggleShuffle();
    }

    [RelayCommand]
    private void CycleRepeat()
    {
        _playbackManager.CycleRepeat();
    }
}