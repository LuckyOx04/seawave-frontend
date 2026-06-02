using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SeawaveApp.ViewModels;

2

public partial class CreatePlaylistViewModel : ViewModelBase
{
    private readonly MainViewModel _mainShell;

    [ObservableProperty] public partial string DialogTitle { get; set; } = string.Empty;
    [ObservableProperty] public partial string PlaylistName { get; set; } = string.Empty;
    
    public bool IsOnlinePlaylist { get; }

    public CreatePlaylistViewModel(MainViewModel mainShell, bool isOnlinePlaylist)
    {
        _mainShell = mainShell;
        IsOnlinePlaylist = isOnlinePlaylist;
        DialogTitle = isOnlinePlaylist ? "Create New Online Playlist" : "Create New Local Playlist";
    }

    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(PlaylistName))
        {
            return;
        }

        string resultName = PlaylistName;
        bool isOnline = IsOnlinePlaylist;
        
        System.Diagnostics.Debug.WriteLine($"Menu result obtained -> Name: {resultName}, Online: {isOnline}");

        CloseDialog();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog();
    }

    private void CloseDialog()
    {
        _mainShell.ActiveOverlay = null;
    }
}