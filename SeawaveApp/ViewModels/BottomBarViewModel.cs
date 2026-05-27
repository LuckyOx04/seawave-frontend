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
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentPositionSeconds))]
    private TimeSpan _currentPosition;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(TrackDurationSeconds))]
    private TimeSpan _trackDuration;
    [ObservableProperty] private bool _isShuffleOn;
    [ObservableProperty] private string _repeatModeLabel = "Repeat: Off";
    [ObservableProperty] private bool _isUserDragging;

    public double CurrentPositionSeconds => CurrentPosition.TotalSeconds;
    public double TrackDurationSeconds => TrackDuration.TotalSeconds;

    public BottomBarViewModel(PlaybackManager playbackManager)
    {
        _playbackManager = playbackManager;

        CurrentTrack = _playbackManager.CurrentTrack;
        IsPlaying = _playbackManager.IsPlaying;
        CurrentPosition = _playbackManager.Position;
        TrackDuration = _playbackManager.Duration;
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
        if (!IsUserDragging)
        {
            CurrentPosition = position;
        }
    }

    private void OnDurationChanged(object? sender, TimeSpan duration)
    {
        TrackDuration = duration;
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
    private void StartDrag()
    {
        IsUserDragging = true;
    }

    [RelayCommand]
    private void SeekToTime(double seconds)
    {
        CurrentPosition = TimeSpan.FromSeconds(seconds);
        _playbackManager.Seek(TimeSpan.FromSeconds(seconds));
        IsUserDragging = false;
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