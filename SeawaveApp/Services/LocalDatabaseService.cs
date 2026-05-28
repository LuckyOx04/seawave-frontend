using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public class LocalDatabaseService
{
    private readonly string _connectionString;

    public LocalDatabaseService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "seawave");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "seawave.db");
        _connectionString = $"Data Source={dbPath}";
        
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Tracks (
                Id TEXT PRIMARY KEY,
                Title TEXT,
                Artist TEXT,
                Album TEXT,
                DurationTicks INTEGER,
                IsRemote INTEGER,
                RemoteUrl TEXT,
                LocalPath TEXT,
                StartOffsetTicks INTEGER
            );

            CREATE TABLE IF NOT EXISTS Playlists (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PlaylistTracks (
                PlaylistId INTEGER,
                TrackId TEXT,
                SortOrder INTEGER,
                FOREIGN KEY(PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE,
                FOREIGN KEY(TrackId) REFERENCES Tracks(Id) ON DELETE CASCADE
            );
            """;
        
        command.ExecuteNonQuery();
    }

    public async Task<List<Playlist>> GetAllPlaylistsAsync()
    {
        var playlists = new List<Playlist>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name FROM Playlists";
        
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            playlists.Add(new Playlist
            {
                Id = reader.GetInt32(0).ToString(),
                Name = reader.GetString(1),
                IsOnline = false
            });
        }

        return playlists;
    }

    public async Task UpsertTracksAsync(IEnumerable<UnifiedTrack> tracks)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            foreach (var track in tracks)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                    """
                    REPLACE INTO Tracks
                    (Id, Title, Artist, Album, DurationTicks, IsRemote, RemoteUrl, LocalPath, StartOffsetTicks)
                    VALUES ($id, $title, $artist, $album, $duration, $isRemote, $remoteUrl, $localPath, $startOffset)
                    """;
                command.Parameters.AddWithValue("$id", track.Id);
                command.Parameters.AddWithValue("$title", track.Title);
                command.Parameters.AddWithValue("$artist", track.Artist);
                command.Parameters.AddWithValue("$album", (object?)track.Album ?? DBNull.Value);
                command.Parameters.AddWithValue("$duration", track.Duration.Ticks);
                command.Parameters.AddWithValue("$isRemote", track.IsRemote ? 1 : 0);
                command.Parameters.AddWithValue("$remoteUrl", (object?)track.RemoteUrl ?? DBNull.Value);
                command.Parameters.AddWithValue("$localPath", (object?)track.LocalPath ?? DBNull.Value);
                command.Parameters.AddWithValue("$startOffset", track.StartOffset.Ticks);
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<int> CretePlaylistAsync(string name)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Playlists (Name) VALUES ($name); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$name", name);
        
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task AddTrackToPlaylistAsync(int playlistId, UnifiedTrack track)
    {
        await UpsertTracksAsync([track]);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO PlaylistTracks (PlaylistId, TrackId) VALUES ($playlistId, $trackId)";
        command.Parameters.AddWithValue("$playlistId", playlistId);
        command.Parameters.AddWithValue("$trackId", track.Id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<UnifiedTrack>> GetPlaylistTracksAsync(int playlistId)
    {
        var tracks = new List<UnifiedTrack>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT t.* FROM Tracks t
            JOIN PlaylistTracks pt ON t.Id = pt.TrackId
            WHERE pt.PlaylistId = $playlistId
            """;
        command.Parameters.AddWithValue("$playlistId", playlistId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tracks.Add(MapTrack(reader));
        }
        
        return tracks;
    }

    private UnifiedTrack MapTrack(SqliteDataReader reader)
    {
        return new UnifiedTrack
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Artist = reader.GetString(2),
            Album = reader.IsDBNull(3) ? null : reader.GetString(3),
            Duration = TimeSpan.FromTicks(reader.GetInt64(4)),
            IsRemote = reader.GetInt32(5) == 1,
            RemoteUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
            LocalPath = reader.IsDBNull(7) ? null : reader.GetString(7),
            StartOffset = TimeSpan.FromTicks(reader.GetInt64(8))
        };
    }
}