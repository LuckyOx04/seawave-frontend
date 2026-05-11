namespace SeawaveApp.Models;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);