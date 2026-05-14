using System.Collections.ObjectModel;

namespace SeawaveApp.Models;

public class Playlist
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }

    public ObservableCollection<UnifiedTrack> Tracks { get; set; } = [];

    public bool CanAddTrack(UnifiedTrack track)
    {
        return !IsOnline || track.IsRemote;
    }
}