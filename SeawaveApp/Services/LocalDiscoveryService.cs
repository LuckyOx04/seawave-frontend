using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class LocalDiscoveryService
{
    private readonly string[] _supportedExtensions = { ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus"};

    public async Task<List<UnifiedTrack>> DiscoverAsync(string path)
    {
        var tracks = new List<UnifiedTrack>();

        if (File.Exists(path))
        {
            var result = await ProcessFile(path);
            if (result != null)
            {
                tracks.AddRange(result);
            }
        }
        else if (Directory.Exists(path))
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var result = await ProcessFile(file);
                if (result != null)
                {
                    tracks.AddRange(result);
                }
            }
        }
        
        return tracks;
    }

    private async Task<List<UnifiedTrack>?> ProcessFile(string filePath)
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
        
        var track = await ExtractMetadata(filePath);
        
        return [track];
    }

    private async Task<UnifiedTrack> ExtractMetadata(string path)
    {
        using var tfile = TagLib.File.Create(path);

        return new UnifiedTrack
        {
            Id = path,
            Title = tfile.Tag.Title ?? Path.GetFileNameWithoutExtension(path),
            Artist = tfile.Tag.FirstPerformer ?? "Unknown Artist",
            Album = tfile.Tag.Album,
            Duration = tfile.Properties.Duration,
            LocalPath = path,
            IsRemote = false
        };
    }

    private List<UnifiedTrack> ParseCueFile(string cuePath)
    {
        var tracks = new List<UnifiedTrack>();
        var lines = File.ReadAllLines(cuePath);
        string? audioFile = null;
        string? albumArtist = null;
        string? albumTitle = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("FILE"))
            {
                audioFile ??= ExtractQuoted(trimmed);
            }
            if (trimmed.StartsWith("PERFORMER"))
            {
                albumArtist ??= ExtractQuoted(trimmed);
            }
            if (trimmed.StartsWith("TITLE"))
            {
                albumTitle ??= ExtractQuoted(trimmed);
            }
        }
        
        return tracks;
    }
    
    private string ExtractQuoted(string input) => Regex.Match(input, "\"(.*)\"").Groups[1].Value;
}