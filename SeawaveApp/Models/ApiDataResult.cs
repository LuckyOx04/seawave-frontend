namespace SeawaveApp.Models;

public record ApiDataResult<T>(bool IsSuccess, T? Data, string? Message);