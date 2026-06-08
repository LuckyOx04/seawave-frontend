using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SeawaveApp.Helpers;
using SeawaveApp.Messages;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ConnectivityChecker _connectivityChecker;
    private readonly AuthStateManager _authStateManager;
    private readonly LibraryManager _libraryManager;
    private readonly ApiService _api;
    
    private static readonly IBrush OfflineBrush = new SolidColorBrush(Color.Parse("#0b0f17"));
    private static readonly IBrush OnlineBrush = new SolidColorBrush(Color.Parse("#007bff"));

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ConnectivityMessage))]
    [NotifyPropertyChangedFor(nameof(ConnectivityBrush))]
    public partial bool IsOnline { get; set; }
    [ObservableProperty] public partial bool IsLoggedIn { get; set; }
    [ObservableProperty] public partial string Username { get; set; }
    [ObservableProperty] public partial CenterContentMode CurrentCenterMode { get; set; } = CenterContentMode.None;
    [ObservableProperty] public partial Playlist? ActiveCenterPlaylist { get; set; }
    [ObservableProperty] public partial bool ShowConnectivityMessage { get; set; }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOverlayActive))]
    public partial ViewModelBase? ActiveOverlay { get; set; }

    public bool IsOverlayActive => ActiveOverlay != null;
    public string ConnectivityMessage => IsOnline ? "Back online" : "Offline mode";
    public IBrush ConnectivityBrush => IsOnline ? OnlineBrush : OfflineBrush;

    public ObservableCollection<UnifiedTrack> TracksSearchResults { get; } = [];
    public ObservableCollection<Playlist> PlaylistsSearchResults { get; } = [];
    public LeftBarViewModel LeftBar { get; }
    public RightBarViewModel RightBar { get; }
    public CenterAreaViewModel CenterArea { get; }
    public TopBarViewModel TopBar { get; }
    public BottomBarViewModel BottomBar { get; }

    public event EventHandler<CenterContentMode>? CenterContentChanged;

    public MainViewModel(ConnectivityChecker connectivityChecker, AuthStateManager authStateManager,
        LibraryManager libraryManager, PlaybackManager playbackManager, ApiService api, 
        IFileDialog fileDialog)
    {
        _connectivityChecker = connectivityChecker;
        _authStateManager = authStateManager;
        _libraryManager = libraryManager;
        _api = api;

        LeftBar = new LeftBarViewModel(this, _libraryManager);
        RightBar = new RightBarViewModel(playbackManager);
        CenterArea = new CenterAreaViewModel(this, libraryManager);
        TopBar = new TopBarViewModel(_api, _libraryManager, this, fileDialog);
        BottomBar = new BottomBarViewModel(playbackManager);

        IsOnline = _connectivityChecker.IsServiceReachable;
        IsLoggedIn = _authStateManager.IsLoggedIn;
        Username = _authStateManager.Username ?? "Guest";
        ShowConnectivityMessage = !IsOnline;

        _connectivityChecker.ConnectivityChanged += OnConnectivityChanged;
        _authStateManager.StateChanged += OnAuthStateChanged;

        _ = _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }

    private async void OnConnectivityChanged(bool isReachable)
    {
        IsOnline = isReachable;
        
        if (IsOnline)
        {
            await ShowOnlineStatusMessage();
        }
        else
        {
            ShowOfflineStatusMessage();
        }
        
        _ = _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }

    private void OnAuthStateChanged()
    {
        IsLoggedIn = _authStateManager.IsLoggedIn;
        Username = _authStateManager.Username ?? "Guest";
        _ = _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }
    
    private async Task ShowOnlineStatusMessage()
    {
        ShowConnectivityMessage = true;
        await Task.Delay(3000);
        ShowConnectivityMessage = false;
    }
    
    private void ShowOfflineStatusMessage()
    {
        ShowConnectivityMessage = true;
    }

    partial void OnCurrentCenterModeChanged(CenterContentMode value)
    {
        if (value != CenterContentMode.PlaylistTracks)
        {
            ActiveCenterPlaylist = null;
        }
        CenterContentChanged?.Invoke(this, value);
    }

    public async Task RefreshDisplayedPlaylists()
    {
        await _libraryManager.RefreshPlaylistsAsync(IsOnline && IsLoggedIn);
    }

    public void NavigateToPlaylist(Playlist? playlist)
    {
        ActiveCenterPlaylist = playlist;
        
        CurrentCenterMode = playlist == null ? CenterContentMode.None : CenterContentMode.PlaylistTracks;
        
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