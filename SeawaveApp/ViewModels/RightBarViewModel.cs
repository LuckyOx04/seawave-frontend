using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class RightBarViewModel : ViewModelBase
{
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty] public partial IList<UnifiedTrack>? DisplayQueue { get; private set; }
    [ObservableProperty] public partial UnifiedTrack? CurrentDisplayTrack { get; private set; }

    public RightBarViewModel(PlaybackManager playbackManager)
    {
        _playbackManager = playbackManager;
        
        UpdateDisplayTracks();

        _playbackManager.ShuffleChanged += (_, _) => UpdateDisplayTracks();
        _playbackManager.QueueChanged += (_, _) => UpdateDisplayTracks();
    }

    private void UpdateDisplayTracks()
    {
        if (_playbackManager.PlaybackOrder.Count == 0 || _playbackManager.OrderIndex < 0)
        {
            CurrentDisplayTrack = null;
            DisplayQueue = new List<UnifiedTrack>();
            return;
        }
        
        var fullPermutationList = new PermutationList(_playbackManager.TracksQueue, _playbackManager.PlaybackOrder);
        CurrentDisplayTrack = _playbackManager.CurrentTrack;

        DisplayQueue = fullPermutationList.Skip(_playbackManager.OrderIndex + 1).ToList();
    }
    
    [RelayCommand]
    private void PlayTrack(UnifiedTrack selectedTrack)
    {
        _playbackManager.PlayFromQueue(selectedTrack);
    }

    [RelayCommand]
    private void RemoveTrackFromQueue(UnifiedTrack? track)
    {
        if (track == null)
        {
            return;
        }
        
        _playbackManager.RemoveTrack(track);
    }

    [RelayCommand]
    private void ClearAllQueue()
    {
        _playbackManager.ClearQueue();
    }
}