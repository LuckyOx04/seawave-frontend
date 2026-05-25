using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ConnectivityService _connectivityService;
    private readonly AuthStateManager _authStateManager;
    private readonly LibraryManager _libraryManager;
    private readonly ApiService _api;

    [ObservableProperty] private bool _isOnline;
    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private string _username = "Guest";
    [ObservableProperty] private CenterContentMode _currentCenterMode = CenterContentMode.None;
    [ObservableProperty] private Playlist? _activeCenterPlaylist;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverlayActive))] 
    private ViewModelBase? _activeOverlay;

    public bool IsOverlayActive => ActiveOverlay != null;

    public ObservableCollection<UnifiedTrack> SearchResults { get; } = [];
    public LeftBarViewModel LeftBar { get; }
    public RightBarViewModel RightBar { get; }
    public CenterAreaViewModel CenterArea { get; }
    public TopBarViewModel TopBar { get; }
    public BottomBarViewModel BottomBar { get; }

    public MainViewModel(ConnectivityService connectivityService, AuthStateManager authStateManager,
        LibraryManager libraryManager, PlaybackManager playbackManager, ApiService api, 
        IFileDialogService fileDialogService)
    {
        _connectivityService = connectivityService;
        _authStateManager = authStateManager;
        _libraryManager = libraryManager;
        _api = api;

        LeftBar = new LeftBarViewModel(this, _libraryManager);
        RightBar = new RightBarViewModel(playbackManager);
        CenterArea = new CenterAreaViewModel(this, playbackManager);
        TopBar = new TopBarViewModel(_api, _libraryManager, this, fileDialogService);
        BottomBar = new BottomBarViewModel(playbackManager);

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
        WeakReferenceMessenger.Default.Send(new PlaylistChangedMessage(playlist));
    }

    [RelayCommand]
    private void OpenLoginOrProfile()
    {
        if (!IsLoggedIn)
        {
            ActiveOverlay = new LoginViewModel(this, _authStateManager);
        }
        else
        {
            ActiveOverlay = new ProfileViewModel(this, _authStateManager, _api);
        }
    }
}