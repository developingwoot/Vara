using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Plugins;

namespace Vara.Api.Hubs;

[Authorize]
public class AnalysisHub(
    IEnhancedKeywordAnalyzer keywordAnalyzer,
    ITrendDetector trendDetector,
    ILogger<AnalysisHub> logger) : Hub
{
    public async Task StartAnalysis(StartAnalysisRequest request)
    {
        var ct = Context.ConnectionAborted;
        var logUserId = Context.ConnectionId;

        var userIdStr = Context.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdStr))
        {
            await SendError("User not authenticated", "UNAUTHENTICATED");
            return;
        }

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            await SendError($"Invalid user id format: {userIdStr}", "INVALID_USER");
            return;
        }

        logUserId = userId.ToString();

        try
        {
            switch (request.Type.ToLowerInvariant())
            {
                case "keyword": await RunKeywordAsync(userId, request, ct); break;
                case "trend":   await RunTrendAsync(userId, request, ct);   break;
                default:
                    await SendError($"Unknown analysis type: {request.Type}", "INVALID_TYPE");
                    break;
            }
        }
        catch (FeatureAccessDeniedException ex) { await SendError(ex.Message, "FEATURE_ACCESS_DENIED"); }
        catch (QuotaExceededException ex)       { await SendError(ex.Message, "QUOTA_EXCEEDED"); }
        catch (OperationCanceledException)      { logger.LogInformation("Analysis cancelled for {UserId}", logUserId); }
        catch (Exception ex)
        {
            logger.LogError(ex, "AnalysisHub error for user {UserId}", logUserId);
            await SendError("Analysis failed unexpectedly", "INTERNAL_ERROR");
        }
    }

    private async Task RunKeywordAsync(Guid userId, StartAnalysisRequest req, CancellationToken ct)
    {
        await SendProgress(1, "Searching YouTube data...", 25, ct);
        if (req.IncludeInsights)
            await SendProgress(2, "Analyzing keyword...", 50, ct);

        var result = await keywordAnalyzer.AnalyzeAsync(
            userId, req.Keyword!, req.Niche, req.IncludeInsights, ct);

        if (req.IncludeInsights)
            await SendProgress(3, "Generating AI insights...", 85, ct);

        await Clients.Caller.SendAsync("AnalysisComplete",
            new { analysisId = Guid.NewGuid(), data = result }, ct);
    }

    private async Task RunTrendAsync(Guid userId, StartAnalysisRequest req, CancellationToken ct)
    {
        await SendProgress(1, "Analyzing trend data...", 50, ct);
        var result = await trendDetector.FindTrendingAsync(userId, req.Niche, ct: ct);
        await Clients.Caller.SendAsync("AnalysisComplete",
            new { analysisId = Guid.NewGuid(), data = result }, ct);
    }

    private Task SendProgress(int step, string stage, int percent, CancellationToken ct) =>
        Clients.Caller.SendAsync("AnalysisProgress",
            new AnalysisProgressMessage(step, stage, percent), ct);

    private Task SendError(string message, string code) =>
        Clients.Caller.SendAsync("AnalysisError", new { message, code });
}
