using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class RightBarViewModel(PlaybackManager playbackManager) : ViewModelBase
{
    public ObservableCollection<UnifiedTrack> QueueTracks => playbackManager.TracksQueue;

    [RelayCommand]
    private void RemoveTrackFromQueue(UnifiedTrack? track)
    {
        if (track != null)
        {
            playbackManager.RemoveTrack(track);
        }
    }

    [RelayCommand]
    private void ClearAllQueue()
    {
        playbackManager.ClearQueue();
    }
}