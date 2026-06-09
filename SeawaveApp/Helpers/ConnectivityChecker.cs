using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace SeawaveApp.Helpers;

public class ConnectivityChecker
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
    } = false;

    public ConnectivityChecker()
    {
        _timer = new Timer(5000);
        _timer.Elapsed += async (_, _) => await CheckConnectionAsync();
        _timer.Start();

        Task.Run(CheckConnectionAsync);
    }

    private async Task CheckConnectionAsync()
    {
        try
        {
            var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head,
                "https://localhost:8081/api/Music/search/tracks?q=test"), 
                HttpCompletionOption.ResponseHeadersRead);
            IsServiceReachable = response.IsSuccessStatusCode || (int)response.StatusCode == 401;
        }
        catch
        {
            IsServiceReachable = false;
        }
    }
}