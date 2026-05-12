using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
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

    private async Task<string> GetMessage(HttpResponseMessage response)
    {
        try
        {
            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return data?.GetValueOrDefault("message") ?? "";
        }
        catch
        {
            return "An unexpected error occurred.";
        }
    }

    public async Task<ApiResult> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/Auth/register", request);
        return new ApiResult(response.IsSuccessStatusCode, await GetMessage(response));
    }

    public async Task<ApiResult> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/Auth/login", request);
        if (!response.IsSuccessStatusCode)
        {
            return new ApiResult(false, await GetMessage(response));
        }

        var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _token = data?.Token;
        if (_token == null)
        {
            return new ApiResult(false, "Token missing from response.");
        }
        SessionStorage.Save(_token);
        UpdateAuthHeader();
        
        return new ApiResult(true, "Login successful.");
    }

    public async Task<ApiResult> LogoutAsync()
    {
        var response = await _httpClient.PostAsync("/api/Auth/logout", null);
        if (!response.IsSuccessStatusCode)
        {
            return new ApiResult(false, await GetMessage(response));
        }
        _token = null;
        SessionStorage.Clear();
        UpdateAuthHeader();
        
        return new ApiResult(true, null);

    }

    public async Task<ApiDataResult<List<TrackDto>>> SearchTracksAsync(string query)
    {
        var response = await _httpClient.GetAsync($"/api/Music/search/tracks?q={query}");
        if (!response.IsSuccessStatusCode)
        {
            return new ApiDataResult<List<TrackDto>>(false, null, await GetMessage(response));
        }

        return new ApiDataResult<List<TrackDto>>(true, await response.Content
            .ReadFromJsonAsync<List<TrackDto>>(), null);
    }

    public async Task<ApiDataResult<List<PlaylistSummaryDto>>> SearchPlaylistsAsync(string query)
    {
        var response = await _httpClient.GetAsync($"/api/Music/search/playlists?q={query}");
        if (!response.IsSuccessStatusCode)
        {
            return new ApiDataResult<List<PlaylistSummaryDto>>(false, null, await GetMessage(response));
        }
        
        return new ApiDataResult<List<PlaylistSummaryDto>>(true, await response.Content
            .ReadFromJsonAsync<List<PlaylistSummaryDto>>(), null);
    }

    public async Task<ApiDataResult<PlaylistDetailsDto?>> GetPlaylistDetailsAsync(int id)
    {
        var response = await _httpClient.GetAsync($"/api/Music/playlist/{id}");
        if (!response.IsSuccessStatusCode)
        {
            return new ApiDataResult<PlaylistDetailsDto?>(false, null, await GetMessage(response));
        }

        return new ApiDataResult<PlaylistDetailsDto?>(true, await response.Content
            .ReadFromJsonAsync<PlaylistDetailsDto>(), null);
    }

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

        var response = await _httpClient.PostAsync("/api/Music/upload", content);

        return response.IsSuccessStatusCode ? new ApiResult(true, null) 
            : new ApiResult(false, await GetMessage(response));
    }

    public async Task<ApiResult> ForgotPasswordAsync(ForgottenPasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/Password/forgot", request);
        
        return new ApiResult(response.IsSuccessStatusCode, await GetMessage(response));
    }

    public async Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync("/api/Password/change", request);

        return new ApiResult(response.IsSuccessStatusCode, response.IsSuccessStatusCode ? "Password changed.":
            await GetMessage(response));
    }

    public async Task<ApiDataResult<List<PlaylistSummaryDto>>> GetUserPlaylistsAsync()
    {
        var response = await _httpClient.GetAsync("/api/Playlist");
        if (!response.IsSuccessStatusCode)
        {
            return new ApiDataResult<List<PlaylistSummaryDto>>(false, null, await GetMessage(response));
        }
        
        return new ApiDataResult<List<PlaylistSummaryDto>>(true, await response.Content
            .ReadFromJsonAsync<List<PlaylistSummaryDto>>(), null);
    }

    public async Task<ApiDataResult<CreatePlaylistResponse>> CreatePlaylistAsync(CreatePlaylistRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/Playlist/create", request);
        if (!response.IsSuccessStatusCode)
        {
            return new ApiDataResult<CreatePlaylistResponse>(false, null, await GetMessage(response));
        }
        
        return new ApiDataResult<CreatePlaylistResponse>(true, await response.Content
            .ReadFromJsonAsync<CreatePlaylistResponse>(), null);
    }

    public async Task<ApiResult> DeletePlaylistAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"/api/Playlist/delete/{id}");
        
        return new ApiResult(response.IsSuccessStatusCode, response.IsSuccessStatusCode ? null 
            : await GetMessage(response));
    }

    public async Task<ApiDataResult<PlaylistDetailsDto>> AddTrackToPlaylistAsync(int playlistId, int trackId)
    {
        var response = await _httpClient.PostAsync($"/api/playlist/{playlistId}/add-track/{trackId}",
            null);
        if (!response.IsSuccessStatusCode)
        {
            return new ApiDataResult<PlaylistDetailsDto>(false, null, await GetMessage(response));
        }
        
        return new ApiDataResult<PlaylistDetailsDto>(true, await response.Content
            .ReadFromJsonAsync<PlaylistDetailsDto>(), null);
    }

    public async Task<ApiDataResult<PlaylistDetailsDto>> RemoveTrackFromPlaylistAsync(int playlistId, int trackId)
    {
        var response = await _httpClient.DeleteAsync(
            $"/api/Playlist/{playlistId}/remove-track/{trackId}");
        if (!response.IsSuccessStatusCode)
        {
            return new ApiDataResult<PlaylistDetailsDto>(false, null, await GetMessage(response));
        }
        
        return new ApiDataResult<PlaylistDetailsDto>(true, await response.Content
            .ReadFromJsonAsync<PlaylistDetailsDto>(), null);
    }
}