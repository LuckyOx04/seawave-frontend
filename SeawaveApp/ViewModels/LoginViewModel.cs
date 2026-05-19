using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeawaveApp.Services;

namespace SeawaveApp.ViewModels;

public partial class LoginViewModel(MainViewModel mainShell, AuthStateManager authStateManager) : ViewModelBase
{
    [ObservableProperty] private string _identifier = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

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
            mainShell.SetOverlay(null);
        }
    }

    [RelayCommand]
    private void NavigateToRegister()
    {
        mainShell.SetOverlay(new RegisterViewModel(mainShell, authStateManager));
    }

    [RelayCommand]
    private void NavigateToForgotPassword()
    {
        mainShell.SetOverlay(new ForgotPasswordViewModel(mainShell));
    }
}