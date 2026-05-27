using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class ChangePasswordViewModel(MainViewModel mainShell, AuthStateManager authStateManager,
    ApiService api) : ViewModelBase
{
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#007bff"));
    private static readonly IBrush FailureBrush = new SolidColorBrush(Color.Parse("#ff6666"));
    
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(StatusColor))]
    private bool _isSuccessState;
    
    public IBrush StatusColor => IsSuccessState ? SuccessBrush : FailureBrush;

    [RelayCommand]
    private async Task ExecuteChangePasswordAsync()
    {
        if (!ValidatorService.IsValidPassword(NewPassword))
        {
            StatusMessage = "Password must have at least 8 characters," +
                                      "an upper case letter, a lower case letter and a digit.";
            IsSuccessState = false;
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            StatusMessage = "Confirmed password does not match the new password.";
            IsSuccessState = false;
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        var request = new ChangePasswordRequest(CurrentPassword, NewPassword, ConfirmPassword);
        var response = await api.ChangePasswordAsync(request);
        
        IsBusy = false;

        if (response.IsSuccess)
        {
            IsSuccessState = true;
            StatusMessage = "Successfully changed password.";
            await Task.Delay(1000);
            Cancel();
        }
        else
        {
            StatusMessage = response.Message!;
            IsSuccessState = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        mainShell.ActiveOverlay = new ProfileViewModel(mainShell, authStateManager, api);
    }
}