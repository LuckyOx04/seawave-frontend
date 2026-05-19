using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class RegisterViewModel(MainViewModel mainShell, AuthStateManager authStateManager) : ViewModelBase
{
    private readonly ApiService _api = new();
    
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isSuccessState;
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    private async Task ExecuteRegisterAsync()
    {
        if (Password != ConfirmPassword)
        {
            StatusMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        var request = new RegisterRequest(Username, Email, Password, ConfirmPassword);
        var result = await _api.RegisterAsync(request);

        IsBusy = false;

        if (result.IsSuccess)
        {
            IsSuccessState = true;
            StatusMessage = result.Message!;
            await Task.Delay(1500);
            NavigateToLogin();
        }
        else
        {
            StatusMessage = result.Message ?? "Registration failed.";
        }
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        mainShell.SetOverlay(new LoginViewModel(mainShell, authStateManager));
    }
}