namespace SeawaveApp.Models;

public record RegisterRequest(string Username, string Email, string Password, string ConfirmPassword);