using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class RightBarViewModel : ViewModelBase
{
    private readonly PlaybackManager _playbackManager;

    [ObservableProperty] public partial PermutationList DisplayQueue { get; private set; }

    public RightBarViewModel(PlaybackManager playbackManager)
    {
        _playbackManager = playbackManager;
        
        UpdateDisplayTracks();

        _playbackManager.ShuffleChanged += (_, _) => UpdateDisplayTracks();
        _playbackManager.TrackChanged += (_, _) => UpdateDisplayTracks();
    }

    private void UpdateDisplayTracks()
    {
        DisplayQueue = new PermutationList(_playbackManager.TracksQueue, _playbackManager.PlaybackOrder);
    }

    [RelayCommand]
    private void RemoveTrackFromQueue(UnifiedTrack? track)
    {
        if (track == null)
        {
            return;
        }
        
        _playbackManager.RemoveTrack(track);
        UpdateDisplayTracks();
    }

    [RelayCommand]
    private void ClearAllQueue()
    {
        _playbackManager.ClearQueue();
        UpdateDisplayTracks();
    }
}