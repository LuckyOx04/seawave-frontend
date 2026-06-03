using System;
using System.Threading.Tasks;
using SeawaveApp.Helpers;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class AuthStateManager(ApiService api)
{
    public event Action? StateChanged;

    public bool IsLoggedIn
    {
        get;
        private set
        {
            field = value;
            StateChanged?.Invoke();
        }
    } = api.IsLoggedInOnStartup;

    public string? Username
    {
        get => field ?? "Guest";
        private set
        {
            field = value;
            StateChanged?.Invoke();
        }
    } = SessionStorage.Load()?.Username;

    public async Task<ApiDataResult<LoginResponse>> Login(string identifier, string password)
    {
        var response = await api.LoginAsync(new LoginRequest(identifier, password));
        if (!response.IsSuccess)
        {
            return response;
        }
        Username = response.Data?.Username;
        IsLoggedIn = true;

        return response;
    }

    public async Task Logout()
    {
        await api.LogoutAsync();
        Username = "Guest";
        IsLoggedIn = false;
    }
}