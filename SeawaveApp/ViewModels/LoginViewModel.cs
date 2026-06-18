using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class LoginViewModel(MainViewModel mainShell, AuthStateManager authStateManager) : ViewModelBase
{
    [ObservableProperty]
    public partial string Identifier { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [RelayCommand]
    private async Task ExecuteLoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Identifier) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Credential fields can't be empty.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var result = await authStateManager.Login(Identifier, Password);
        IsBusy = false;

        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message ?? "Authorization failed.";
        }
        else
        {
            mainShell.ActiveOverlay = null;
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        mainShell.ActiveOverlay = null;
    }
    
    [RelayCommand]
    private void NavigateToRegister()
    {
        mainShell.ActiveOverlay = new RegisterViewModel(mainShell, authStateManager);
    }

    [RelayCommand]
    private void NavigateToForgotPassword()
    {
        mainShell.ActiveOverlay = new ForgotPasswordViewModel(mainShell, authStateManager);
    }
}