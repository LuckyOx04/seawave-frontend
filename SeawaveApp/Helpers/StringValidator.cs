using System.Text.RegularExpressions;

namespace SeawaveApp.Helpers;

public static partial class StringValidator
{
    public static bool IsValidEmail(string email) 
        => AllowedEmailsRegex().IsMatch(email);

    public static bool IsValidPassword(string password) 
        => AllowedPasswordsRegex().IsMatch(password);

    public static bool IsValidUsername(string username)
        => AllowedUsernamesRegex().IsMatch(username);
    
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex AllowedEmailsRegex();
    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$")]
    private static partial Regex AllowedPasswordsRegex();
    [GeneratedRegex("^[^@]*$")]
    private static partial Regex AllowedUsernamesRegex();
}