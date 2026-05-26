using System;
using System.IO;
using System.Text.Json;
using SeawaveApp.Models;

namespace SeawaveApp.Services;

public static class SessionStorage
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "seawave", "session.json");

    public static void Save(string token, string? username)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var session = new UserSession(token, DateTime.UtcNow, username);
        File.WriteAllText(Path, JsonSerializer.Serialize(session));
    }

    public static UserSession? Load()
    {
        if (!File.Exists(Path))
        {
            return null;
        }

        var session = JsonSerializer.Deserialize<UserSession>(File.ReadAllText(Path));

        if (session != null && !((DateTime.UtcNow - session.CreatedAt).TotalDays > 7))
        {
            return session;
        }
        
        File.Delete(Path);
        return null;
    }
    
    public static void Clear() => File.Delete(Path);
}