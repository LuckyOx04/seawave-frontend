using System.Collections.ObjectModel;
using System.Threading.Tasks;
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

    [ObservableProperty]
    public partial bool IsOnline { get; set; }

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    [ObservableProperty]
    public partial string Username { get; set; }

    [ObservableProperty]
    public partial CenterContentMode CurrentCenterMode { get; set; } = CenterContentMode.None;

    [ObservableProperty]
    public partial Playlist? ActiveCenterPlaylist { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverlayActive))]
    public partial ViewModelBase? ActiveOverlay { get; set; }

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

    public async Task RefreshDisplayedPlaylists()
    {
        await _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }

    public async Task NavigateToPlaylist(Playlist? playlist)
    {
        ActiveCenterPlaylist = playlist;
        
        if (playlist == null)
        {
            CurrentCenterMode = CenterContentMode.None;
            await _libraryManager.ClearTemporaryPlaylistAsync();
        }
        else if (playlist == _libraryManager.TemporaryPlaylist)
        {
            CurrentCenterMode = CenterContentMode.TemporaryTracks;
        }
        else
        {
            CurrentCenterMode = CenterContentMode.PlaylistTracks;
        }
        
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