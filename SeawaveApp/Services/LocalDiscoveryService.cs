using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SeawaveApp.Helpers;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class LocalDiscoveryService
{
    private readonly string[] _supportedExtensions = [".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus"];

    public async Task<List<UnifiedTrack>> DiscoverAsync(string path)
    {
        var tracks = new List<UnifiedTrack>();

        if (File.Exists(path))
        {
            var result = await ProcessFileAsync(path);
            if (result != null)
            {
                tracks.AddRange(result);
            }
        }
        else if (Directory.Exists(path))
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var result = await ProcessFileAsync(file);
                if (result != null)
                {
                    tracks.AddRange(result);
                }
            }
        }
        
        return tracks;
    }

    private async Task<List<UnifiedTrack>?> ProcessFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();

        if (ext == ".cue")
        {
            return ParseCueFile(filePath);
        }
        
        if (!_supportedExtensions.Contains(ext))
        {
            return null;
        }
        
        var track = await ExtractMetadataAsync(filePath);
        
        return [track];
    }

    private static async Task<UnifiedTrack> ExtractMetadataAsync(string path)
    {
        return await Task.Run(() =>
        {
            using var tfile = TagLib.File.Create(path);
            return new UnifiedTrack
            {
                Id = IdGenerator.GenerateSha256Id(path),
                Title = tfile.Tag.Title ?? Path.GetFileNameWithoutExtension(path),
                Artist = tfile.Tag.FirstPerformer ?? "Unknown Artist",
                Album = tfile.Tag.Album,
                Duration = tfile.Properties.Duration,
                LocalPath = path,
                IsRemote = false
            };
        });
    }
    
    private static List<UnifiedTrack> ParseCueFile(string cuePath)
    {
        var tracks = new List<UnifiedTrack>();
        var lines = File.ReadAllLines(cuePath);
        
        string? currentAudioFile = null;
        string? albumArtist = null;
        string? albumTitle = null;
        
        UnifiedTrack? currentTrack = null;
        var directory = Path.GetDirectoryName(cuePath) ?? "";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            var parts = trimmed.Split(' ', 2);
            if (parts.Length < 2)
            {
                continue;
            }
            
            var key = parts[0].ToUpper();
            var value = parts[1].Trim('"');

            switch (key)
            {
                case "FILE":
                    currentAudioFile = Path.Combine(directory, value[..value.LastIndexOf(' ')].Trim('"'));
                    break;
                case "PERFORMER":
                    if (currentTrack == null)
                    {
                        albumArtist = value;
                    }
                    else
                    {
                        currentTrack = currentTrack with { Artist = value };
                    }
                    break;
                case "TITLE":
                    if (currentTrack == null)
                    {
                        albumTitle = value;
                    }
                    else
                    {
                        currentTrack = currentTrack with { Title = value };
                    }
                    break;
                case "TRACK":
                    if (currentTrack != null)
                    {
                        tracks.Add(currentTrack);
                    }

                    value = value.Split(' ')[0];
                    
                    currentTrack = new UnifiedTrack
                    {
                        Id = IdGenerator.GenerateSha256Id($"{cuePath}_{value}"),
                        Artist = albumArtist ?? "Unknown Artist",
                        Album = albumTitle,
                        LocalPath = currentAudioFile,
                        IsRemote = false
                    };
                    break;
                case "INDEX":
                    if (currentTrack != null && value.StartsWith("01"))
                    {
                        var timeString = value.Split(' ')[1];
                        currentTrack = currentTrack with { StartOffset = ParseCueTime(timeString) };
                    }
                    break;
            }
        }

        if (currentTrack != null)
        {
            tracks.Add(currentTrack);
        }

        for (var i = 0; i < tracks.Count; i++)
        {
            if (i < tracks.Count - 1)
            {
                var duration = tracks[i + 1].StartOffset - tracks[i].StartOffset;
                tracks[i] = tracks[i] with { Duration = duration };
            }
            else if (File.Exists(currentAudioFile))
            {
                using var tfile = TagLib.File.Create(currentAudioFile);
                tracks[i] = tracks[i] with { Duration = tfile.Properties.Duration - tracks[i].StartOffset };
            }
        }
        
        return tracks;
    }

    private static TimeSpan ParseCueTime(string time)
    {
        var parts = time.Split(':');
        if (parts.Length != 3)
        {
            return TimeSpan.Zero;
        }
        
        var m = int.Parse(parts[0]);
        var s = int.Parse(parts[1]);
        var f = int.Parse(parts[2]);

        var totalSeconds = (m * 60) + s + (f / 75.0);
        return TimeSpan.FromSeconds(totalSeconds);
    }
}