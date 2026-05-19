using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class ForgotPasswordViewModel(MainViewModel mainShell, 
    AuthStateManager authStateManager) : ViewModelBase
{
    private readonly ApiService _api = new();
    
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    private async Task ExecuteResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            StatusMessage = "Please enter a valid email address.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        var request = new ForgottenPasswordRequest(Email);
        var result = await _api.ForgotPasswordAsync(request);
        
        StatusMessage = result.Message!;
        IsBusy = false;
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        mainShell.ActiveOverlay = new LoginViewModel(mainShell, authStateManager);
    }
}