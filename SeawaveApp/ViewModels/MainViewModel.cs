using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ConnectivityService _connectivityService;
    private readonly AuthStateManager _authStateManager;
    private readonly LibraryManager _libraryManager;

    [ObservableProperty] private bool _isOnline;
    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private string _username = "Guest";
    [ObservableProperty] private CenterContentMode _currentCenterMode = CenterContentMode.None;
    [ObservableProperty] private Playlist? _activeCenterPlaylist;

    public ObservableCollection<UnifiedTrack> SearchResults { get; } = [];
    public LeftBarViewModel LeftBar { get; }
    public RightBarViewModel RightBar { get; }
    public CenterAreaViewModel CenterArea { get; }

    public MainViewModel(ConnectivityService connectivityService, AuthStateManager authStateManager,
        LibraryManager libraryManager, PlaybackManager playbackManager)
    {
        _connectivityService = connectivityService;
        _authStateManager = authStateManager;
        _libraryManager = libraryManager;

        LeftBar = new LeftBarViewModel(this, _libraryManager);
        RightBar = new RightBarViewModel(playbackManager);
        CenterArea = new CenterAreaViewModel(this, playbackManager);

        IsOnline = _connectivityService.IsServiceReachable;
        IsLoggedIn = _authStateManager.IsLoggedIn;
        Username = _authStateManager.Username ?? "Guest";

        _connectivityService.ConnectivityChanged += OnConnectivityChanged;
        _authStateManager.StateChanged += OnAuthStateChanged;

        _ = _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }

    private void OnConnectivityChanged(bool isReachable)
    {
        IsOnline = isReachable;
        _ = _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }

    private void OnAuthStateChanged()
    {
        IsLoggedIn = _authStateManager.IsLoggedIn;
        Username = _authStateManager.Username ?? "Guest";
        _ = _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }

    public void NavigateToPlaylist(Playlist playlist)
    {
        ActiveCenterPlaylist = playlist;
        CurrentCenterMode = playlist == _libraryManager.TemporaryPlaylist
            ? CenterContentMode.TemporaryTracks
            : CenterContentMode.PlaylistTracks;
    }
}