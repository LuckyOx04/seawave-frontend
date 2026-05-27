using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class RegisterViewModel(MainViewModel mainShell, AuthStateManager authStateManager) : ViewModelBase
{
    private readonly ApiService _api = new();
    
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#007bff"));
    private static readonly IBrush FailureBrush = new SolidColorBrush(Color.Parse("#ff6666"));

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(StatusColor))]
    private bool _isSuccessState;

    [ObservableProperty] private bool _isBusy;

    public IBrush StatusColor => IsSuccessState ? SuccessBrush : FailureBrush;

    [RelayCommand]
    private async Task ExecuteRegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || !ValidatorService.IsValidUsername(Username))
        {
            StatusMessage = "Username cannot be empty or contain @ symbol.";
            IsSuccessState = false;
            return;
        }

        if (!ValidatorService.IsValidEmail(Email))
        {
            StatusMessage = "Invalid email format.";
            IsSuccessState = false;
            return;
        }

        if (!ValidatorService.IsValidPassword(Password))
        {
            StatusMessage = "Password must have at least 8 characters," +
                            "an upper case letter, a lower case letter and a digit.";
            IsSuccessState = false;
            return;
        }

        if (Password != ConfirmPassword)
        {
            StatusMessage = "Passwords do not match.";
            IsSuccessState = false;
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        var request = new RegisterRequest(Username, Email, Password, ConfirmPassword);
        var result = await _api.RegisterAsync(request);

        IsBusy = false;

        if (result.IsSuccess)
        {
            Username = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            IsSuccessState = true;
            StatusMessage = result.Message!;
        }
        else
        {
            StatusMessage = result.Message ?? "Registration failed.";
            IsSuccessState = false;
        }
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        mainShell.ActiveOverlay = new LoginViewModel(mainShell, authStateManager);
    }
}