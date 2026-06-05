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
                Title TEXT NOT NULL,
                Artist TEXT,
                Album TEXT,
                DurationSeconds REAL NOT NULL,
                IsRemote INTEGER DEFAULT 0,
                RemoteUrl TEXT UNIQUE,
                LocalPath TEXT UNIQUE,
                StartOffset REAL
            );

            CREATE TABLE IF NOT EXISTS Playlists (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PlaylistTracks (
                PlaylistId INTEGER NOT NULL,
                TrackId TEXT NOT NULL,
                PRIMARY KEY(PlaylistId, TrackId),
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
                Id = reader.GetString(0),
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
                    (Id, Title, Artist, Album, DurationSeconds, IsRemote, RemoteUrl, LocalPath, StartOffset)
                    VALUES ($id, $title, $artist, $album, $duration, $isRemote, $remoteUrl, $localPath, $startOffset);
                    """;
                command.Parameters.AddWithValue("$id", track.Id);
                command.Parameters.AddWithValue("$title", track.Title);
                command.Parameters.AddWithValue("$artist", track.Artist);
                command.Parameters.AddWithValue("$album", (object?)track.Album ?? DBNull.Value);
                command.Parameters.AddWithValue("$duration", track.Duration.TotalSeconds);
                command.Parameters.AddWithValue("$isRemote", track.IsRemote ? 1 : 0);
                command.Parameters.AddWithValue("$remoteUrl", (object?)track.RemoteUrl ?? DBNull.Value);
                command.Parameters.AddWithValue("$localPath", (object?)track.LocalPath ?? DBNull.Value);
                command.Parameters.AddWithValue("$startOffset", track.StartOffset.TotalSeconds);
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

    public async Task CretePlaylistAsync(string id, string name)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Playlists (Id, Name) VALUES ($id, $name);";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeletePlaylistAsync(string id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Playlists WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        await command.ExecuteNonQueryAsync();
    }

    public async Task AddTrackToPlaylistAsync(string playlistId, UnifiedTrack track)
    {
        await UpsertTracksAsync([track]);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText =
            "INSERT OR IGNORE INTO PlaylistTracks (PlaylistId, TrackId) VALUES ($playlistId, $trackId);";
        command.Parameters.AddWithValue("$playlistId", playlistId);
        command.Parameters.AddWithValue("$trackId", track.Id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveTrackFromPlaylistAsync(string playlistId, UnifiedTrack track)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM PlaylistTracks WHERE PlaylistId = $playlistId AND TrackId = $trackId;";
        command.Parameters.AddWithValue("$playlistId", playlistId);
        command.Parameters.AddWithValue("$trackId", track.Id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<List<UnifiedTrack>> GetPlaylistTracksAsync(string playlistId)
    {
        var tracks = new List<UnifiedTrack>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT t.* FROM Tracks t
            JOIN PlaylistTracks pt ON t.Id = pt.TrackId
            WHERE pt.PlaylistId = $playlistId;
            """;
        command.Parameters.AddWithValue("$playlistId", playlistId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tracks.Add(MapTrack(reader));
        }

        return tracks;
    }

    private static UnifiedTrack MapTrack(SqliteDataReader reader)
    {
        return new UnifiedTrack
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Artist = reader.GetString(2),
            Album = reader.IsDBNull(3) ? null : reader.GetString(3),
            Duration = TimeSpan.FromSeconds(reader.GetDouble(4)),
            IsRemote = reader.GetInt32(5) == 1,
            RemoteUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
            LocalPath = reader.IsDBNull(7) ? null : reader.GetString(7),
            StartOffset = TimeSpan.FromSeconds(reader.GetDouble(8))
        };
    }
}