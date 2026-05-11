using System;

namespace SeawaveApp.Models;

public record UserSession(string Token, DateTime CreatedAt);