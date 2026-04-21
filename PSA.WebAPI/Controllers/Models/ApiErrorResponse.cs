namespace PSA.WebAPI.Controllers.Models;

public sealed class ApiErrorResponse
{
    public bool Success { get; init; } = false;
    public string Message { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = [];
    public string? TraceId { get; init; }
}
