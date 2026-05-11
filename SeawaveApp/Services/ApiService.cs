using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Data;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public ApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7212") };
        _token = SessionStorage.Load();
        UpdateAuthHeader();
    }

    private void UpdateAuthHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            !string.IsNullOrEmpty(_token) ? new AuthenticationHeaderValue(_token) : null;
    }

    public async Task<bool> LoginAsync(string identifier, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/Auth/login", new LoginRequest(identifier, 
            password));
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _token = data?.Token;
        if (_token == null)
        {
            return false;
        }
        SessionStorage.Save(_token);
        UpdateAuthHeader();
        
        return true;
    }

    public async Task LogoutAsync()
    {
        await _httpClient.PostAsync("/api/Auth/logout", null);
        _token = null;
        SessionStorage.Clear();
        UpdateAuthHeader();
    }

    public async Task<List<TrackDto>> SearchTracksAsync(string query)
        => await _httpClient.GetFromJsonAsync<List<TrackDto>>($"/api/Music/search/tracks?q={query}") ?? [];
    
    public async Task<List<PlaylistSummaryDto>> SearchPlaylistsAsync(string query)
        => await _httpClient.GetFromJsonAsync<List<PlaylistSummaryDto>>(
            $"/api/Music/search/playlists?q={query}") ?? [];
    
    public async Task<PlaylistDetailsDto?> GetPlaylistDetailsAsync(int id)
        => await _httpClient.GetFromJsonAsync<PlaylistDetailsDto>($"/api/Music/playlist/{id}");

    public async Task<List<PlaylistSummaryDto>> GetUserPlaylistsAsync()
        => await _httpClient.GetFromJsonAsync<List<PlaylistSummaryDto>>("/api/Playlist") ?? [];

    public async Task<bool> CreatePlaylistAsync(string name)
    {
        var result = await _httpClient.PostAsJsonAsync("/api/Playlist/create", 
            new CreatePlaylistRequest(name));
        return result.IsSuccessStatusCode;
    }

    public async Task<bool> UploadTrackAsync(string title, string artist, string filePath)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "title");
        content.Add(new StringContent(artist), "artist");
        
        var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var result = await _httpClient.PostAsync("/api/Music/upload", content);
        return result.IsSuccessStatusCode;
    }

    public string GetStreamUrl(string fileName) => $"{_httpClient.BaseAddress}api/Music/stream/{fileName}";
    
    
}