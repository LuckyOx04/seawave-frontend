using System;

namespace SeawaveApp.Models;

public record UserProfileResponse(string Username, string Email, DateTimeOffset CreatedAt, int CreatedPlaylistsCount,
    int PendingTracksCount, int ApprovedTracksCount);