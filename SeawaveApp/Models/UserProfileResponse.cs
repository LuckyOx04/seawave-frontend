using System;

namespace SeawaveApp.Models;

public record UserProfileResponse(
    string Username,
    string Email,
    DateTimeOffset CreatedAt,
    long CreatedPlaylistsCount,
    long PendingTracksCount,
    long ApprovedTracksCount);
