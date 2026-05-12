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

    public async Task<ApiResult> RegisterAsync(RegisterRequest request)
    {
        var result = await _httpClient.PostAsJsonAsync("/api/Auth/register", request);
        var content = await result.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var message = content?.GetValueOrDefault("message") ?? "Server error";
        
        return new ApiResult(result.IsSuccessStatusCode, message);
    }

    public async Task<ApiResult> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/Auth/login", request);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        if (!response.IsSuccessStatusCode)
        {
            var message = content?.GetValueOrDefault("message") ?? "Server error";
            return new ApiResult(false, message);
        }

        _token = content?.GetValueOrDefault("token");
        if (_token == null)
        {
            return new ApiResult(false, "Server error");
        }
        SessionStorage.Save(_token);
        UpdateAuthHeader();
        
        return new ApiResult(true, null);
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
    
    public string GetStreamUrl(string fileName) => $"{_httpClient.BaseAddress}api/Music/stream/{fileName}";

    public async Task<ApiResult> UploadTrackAsync(string title, string artist, string filePath)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "title");
        content.Add(new StringContent(artist), "artist");
        
        var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        var result = await _httpClient.PostAsync("/api/Music/upload", content);
        if (result.IsSuccessStatusCode)
        {
            return new ApiResult(true, null);
        }
        var contents = await result.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var message = contents?.GetValueOrDefault("message") ?? "Server error";
        
        return new ApiResult(false, message);
    }

    public async Task<ApiResult> ForgotPasswordAsync(ForgottenPasswordRequest request)
    {
        var res = await _httpClient.PostAsJsonAsync("/api/Password/forgot", request);
        var content = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var message = content?.GetValueOrDefault("message") ?? "Server error";
        
        return new ApiResult(res.IsSuccessStatusCode, message);
    }

    public async Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var result = await _httpClient.PatchAsJsonAsync("/api/Password/change", request);
        if (result.IsSuccessStatusCode)
        {
            return new ApiResult(true, null);
        }
        var content = await result.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var message = content?.GetValueOrDefault("message") ?? "Server error";
        
        return new ApiResult(false, message);
    }
    
    public async Task<List<PlaylistSummaryDto>> GetUserPlaylistsAsync()
        => await _httpClient.GetFromJsonAsync<List<PlaylistSummaryDto>>("/api/Playlist") ?? [];

    public async Task<ApiResult> CreatePlaylistAsync(CreatePlaylistRequest request)
    {
        var result = await _httpClient.PostAsJsonAsync("/api/Playlist/create", request);
        var content = await result.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        if (result.IsSuccessStatusCode)
        {
            return new ApiResult(true, null);
        }
        var message = content?.GetValueOrDefault("message") ?? "Server error";
        return new ApiResult(false, message);
    }

    public async Task<ApiResult> DeletePlaylistAsync(int id)
    {
        var result = await _httpClient.DeleteAsync($"/api/Playlist/delete/{id}");
        if (result.IsSuccessStatusCode)
        {
            return new ApiResult(true, null);
        }
        var content = await result.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var message = content?.GetValueOrDefault("message");
        
        return new ApiResult(false, message);
    }

    public async Task<PlaylistDetailsDto?> AddTrackToPlaylistAsync(int playlistId, int trackId)
    {
        var result = await _httpClient.PostAsync(
            $"/api/playlist/{playlistId}/add-track/{trackId}", null);

        return result.IsSuccessStatusCode ? await result.Content.ReadFromJsonAsync<PlaylistDetailsDto>() : null;
    }

    public async Task<PlaylistDetailsDto?> RemoveTrackFromPlaylistAsync(int playlistId, int trackId)
    {
        var result = await _httpClient.DeleteAsync(
            $"/api/Playlist/{playlistId}/remove-track/{trackId}");
        return result.IsSuccessStatusCode ? await result.Content.ReadFromJsonAsync<PlaylistDetailsDto>() : null;
    }
}