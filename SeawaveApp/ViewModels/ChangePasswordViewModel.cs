using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Helpers;
using SeawaveApp.Models;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class ChangePasswordViewModel(
    MainViewModel mainShell,
    AuthStateManager authStateManager,
    ApiService api) : ViewModelBase
{
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#007bff"));
    private static readonly IBrush FailureBrush = new SolidColorBrush(Color.Parse("#ff6666"));

    [ObservableProperty] public partial string CurrentPassword { get; set; } = string.Empty;
    [ObservableProperty] public partial string NewPassword { get; set; } = string.Empty;
    [ObservableProperty] public partial string ConfirmPassword { get; set; } = string.Empty;
    [ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private partial bool IsSuccessState { get; set; }

    public IBrush StatusColor => IsSuccessState ? SuccessBrush : FailureBrush;

    [RelayCommand]
    private async Task ExecuteChangePasswordAsync()
    {
        if (!StringValidator.IsValidPassword(NewPassword))
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