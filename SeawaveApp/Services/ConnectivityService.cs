using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace SeawaveApp.Services;

public class ConnectivityService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };

    private readonly Timer _timer;

    public event Action<bool>? ConnectivityChanged;

    public bool IsServiceReachable
    {
        get;
        private set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            ConnectivityChanged?.Invoke(field);
        }
    }

    public ConnectivityService()
    {
        _timer = new Timer(5000);
        _timer.Elapsed += async (s, e) => await CheckConnectionAsync();
        _timer.Start();

        Task.Run(CheckConnectionAsync);
    }

    private async Task CheckConnectionAsync()
    {
        try
        {
            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head,
                "https://localhost:7212/api/Music/search/tracks?q=test"));
            IsServiceReachable = response.IsSuccessStatusCode || (int)response.StatusCode == 401;
        }
        catch
        {
            IsServiceReachable = false;
        }
    }
}