using System;
using System.Security.Cryptography;
using System.Text;

namespace SeawaveApp.Services;

public static class IdGenerator
{
    public static string GenerateSha256Id(string inputString)
    {
        var inputBytes = Encoding.UTF8.GetBytes(inputString);

        var hashBytes = SHA256.HashData(inputBytes);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}