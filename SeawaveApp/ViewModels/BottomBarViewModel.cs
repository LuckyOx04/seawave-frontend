using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class BottomBarViewModel : ViewModelBase
{
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty] public partial UnifiedTrack? CurrentTrack { get; set; }
    [ObservableProperty] public partial bool IsPlaying { get; set; }
    [ObservableProperty] public partial TimeSpan CurrentPosition { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackDurationSeconds))]
    public partial TimeSpan TrackDuration { get; set; }

    [ObservableProperty] public partial bool IsShuffleOn { get; set; }
    [ObservableProperty] public partial string RepeatModeLabel { get; set; } = "Repeat: Off";

    private bool _isUserDragging;

    public double SliderValue
    {
        get => CurrentPosition.TotalSeconds;
        set
        {
            if (Math.Abs(value - CurrentPosition.TotalSeconds) <= 1)
            {
                SkipNext();
            }
            else if (!_isUserDragging)
            {
                CurrentPosition = TimeSpan.FromSeconds(value);
            }
        }
    }

    public double TrackDurationSeconds => CurrentTrack != null
        ? TrackDuration.TotalSeconds
        : 0.1;

    public BottomBarViewModel(PlaybackManager playbackManager)
    {
        _playbackManager = playbackManager;

        CurrentTrack = _playbackManager.CurrentTrack;
        IsPlaying = _playbackManager.PlaybackState == MediaPlayerState.Playing;
        CurrentPosition = CalculateCurrentPosition(_playbackManager.Position) ?? TimeSpan.Zero;
        TrackDuration = CurrentTrack?.Duration ?? _playbackManager.Duration;
        IsShuffleOn = _playbackManager.IsShuffle;
        UpdateRepeatLabel(_playbackManager.CurrentRepeatMode);

        _playbackManager.TrackChanged += (_, track) => CurrentTrack = track;
        _playbackManager.PlaybackStateChanged += (_, _) => 
            IsPlaying = _playbackManager.PlaybackState == MediaPlayerState.Playing;
        _playbackManager.ShuffleChanged += (_, shuffle) => IsShuffleOn = shuffle;
        _playbackManager.RepeatChanged += (_, mode) => UpdateRepeatLabel(mode);

        _playbackManager.PositionChanged += OnPositionChanged;
        _playbackManager.DurationChanged += OnDurationChanged;
    }

    private TimeSpan? CalculateCurrentPosition(TimeSpan playbackManagerPosition)
    {
        return playbackManagerPosition - CurrentTrack?.StartOffset;
    }

    private void OnPositionChanged(object? sender, TimeSpan position)
    {
        if (_isUserDragging)
        {
            return;
        }

        CurrentPosition = CalculateCurrentPosition(position) ?? TimeSpan.Zero;
        OnPropertyChanged(nameof(SliderValue));
    }

    private void OnDurationChanged(object? sender, TimeSpan duration)
    {
        TrackDuration = CurrentTrack?.Duration ?? duration;
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
        _isUserDragging = true;
    }

    [RelayCommand]
    private void SeekToTime(double seconds)
    {
        CurrentPosition = TimeSpan.FromSeconds(seconds);
        _playbackManager.Seek(CurrentTrack!.StartOffset + TimeSpan.FromSeconds(seconds));
        _isUserDragging = false;
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
        if (SliderValue >= 2)
        {
            SeekToTime(0.0);
        }
        else
        {
            _playbackManager.Previous();
        }
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