namespace Vara.Api.Hubs;

public class StartAnalysisRequest
{
    public string Type { get; set; } = string.Empty;
    public string? Keyword { get; set; }
    public string? Niche { get; set; }
    public bool IncludeInsights { get; set; }
}

public record AnalysisProgressMessage(int Step, string Stage, int Percent);
