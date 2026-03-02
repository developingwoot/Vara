namespace Vara.Api.Models;

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
