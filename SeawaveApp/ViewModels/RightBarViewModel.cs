using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class RightBarViewModel : ViewModelBase
{
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty] public partial IList<UnifiedTrack> DisplayQueue { get; private set; }

    public RightBarViewModel(PlaybackManager playbackManager)
    {
        _playbackManager = playbackManager;
        
        UpdateDisplayTracks();

        _playbackManager.ShuffleChanged += OnPlaybackManagerShuffleChanged;
    }

    private void OnPlaybackManagerShuffleChanged(object? sender, bool isShuffleOn)
    {
        UpdateDisplayTracks();
    }

    private void UpdateDisplayTracks()
    {
        DisplayQueue = new PermutationList(_playbackManager.TracksQueue, _playbackManager.PlaybackOrder);
    }

    [RelayCommand]
    private void RemoveTrackFromQueue(UnifiedTrack? track)
    {
        if (track != null)
        {
            _playbackManager.RemoveTrack(track);
        }
    }

    [RelayCommand]
    private void ClearAllQueue()
    {
        _playbackManager.ClearQueue();
    }
}