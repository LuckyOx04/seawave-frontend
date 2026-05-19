using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class ChangePasswordViewModel(MainViewModel mainShell, AuthStateManager authStateManager,
    ApiService api) : ViewModelBase
{
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [RelayCommand]
    private async Task ExecuteChangePasswordAsync()
    {
        if (!ValidatorService.IsValidPassword(NewPassword))
        {
            StatusMessage = "Password must have at least 8 characters," +
                                      "an upper case letter, a lower case letter and a digit.";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            StatusMessage = "Confirmed password does not match the new password.";
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        var request = new ChangePasswordRequest(CurrentPassword, NewPassword, ConfirmPassword);
        var response = await api.ChangePasswordAsync(request);
        
        IsBusy = false;

        if (response.IsSuccess)
        {
            StatusMessage = "Successfully changed password.";
            await Task.Delay(1000);
            Cancel();
        }
        else
        {
            StatusMessage = response.Message!;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        mainShell.ActiveOverlay = new ProfileViewModel(mainShell, authStateManager, api);
    }
}