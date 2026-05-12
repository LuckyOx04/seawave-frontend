using System;

namespace SeawaveApp.Models;

public record UnifiedTrack()
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = "Unknown Title";
    public string Artist { get; init; } = "Unknown Artist";
    public string? Album { get; init; }
    public TimeSpan Duration { get; init; }
    
    public bool IsRemote { get; init; }
    public string? RemoteUrl { get; init; }
    public string? LocalPath { get; init; }
    
    public TimeSpan StartOffset { get; init; } = TimeSpan.Zero;
}