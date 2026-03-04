using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;

namespace Vara.Api.Services.YouTube;

public record VideoAnalyticsData(
    string VideoId,
    long? Views,
    double? EstimatedMinutesWatched,
    double? AverageViewPercentage,
    double? AverageViewDurationSeconds,
    double? ImpressionClickThroughRate);

public interface IYouTubeAnalyticsClient
{
    /// <summary>Returns analytics data for a set of video IDs using the user's stored OAuth token.</summary>
    Task<Dictionary<string, VideoAnalyticsData>> GetVideoAnalyticsAsync(
        Guid userId, IEnumerable<string> videoIds, CancellationToken ct = default);

    /// <summary>Returns true if the user has a valid (non-expired) analytics token.</summary>
    Task<bool> IsConnectedAsync(Guid userId, CancellationToken ct = default);
}

public class YouTubeAnalyticsClient(
    IHttpClientFactory httpClientFactory,
    VaraContext db,
    IConfiguration config,
    ILogger<YouTubeAnalyticsClient> logger) : IYouTubeAnalyticsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string AnalyticsBaseUrl = "https://youtubeanalytics.googleapis.com/v2/reports";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    public async Task<bool> IsConnectedAsync(Guid userId, CancellationToken ct = default)
    {
        var token = await db.YouTubeOAuthTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

        if (token is null) return false;

        // Consider "connected" if token hasn't expired or we have a refresh token
        return token.ExpiresAt > DateTime.UtcNow.AddMinutes(5) || token.RefreshToken is not null;
    }

    public async Task<Dictionary<string, VideoAnalyticsData>> GetVideoAnalyticsAsync(
        Guid userId, IEnumerable<string> videoIds, CancellationToken ct = default)
    {
        var token = await db.YouTubeOAuthTokens
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

        if (token is null)
            return [];

        // Refresh token if expired
        if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5) && token.RefreshToken is not null)
        {
            var refreshed = await RefreshAccessTokenAsync(token.RefreshToken, ct);
            if (refreshed is null)
            {
                logger.LogWarning("Failed to refresh YouTube OAuth token for user {UserId}", userId);
                return [];
            }
            token.AccessToken = refreshed.AccessToken;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn - 30);
            await db.SaveChangesAsync(ct);
        }

        var videoList = videoIds.ToList();
        if (videoList.Count == 0) return [];

        // YouTube Analytics API: query up to 50 videos at a time
        var result = new Dictionary<string, VideoAnalyticsData>();
        var batches = videoList.Chunk(50);

        foreach (var batch in batches)
        {
            var batchData = await FetchBatchAsync(token.AccessToken, batch, ct);
            foreach (var (k, v) in batchData) result[k] = v;
        }

        return result;
    }

    private async Task<Dictionary<string, VideoAnalyticsData>> FetchBatchAsync(
        string accessToken, IEnumerable<string> videoIds, CancellationToken ct)
    {
        var ids = string.Join(",", videoIds.Select(id => $"video=={id}"));
        var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var startDate = DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-dd");

        var url = $"{AnalyticsBaseUrl}?ids=channel==MINE" +
                  $"&startDate={startDate}&endDate={endDate}" +
                  $"&metrics=views,estimatedMinutesWatched,averageViewPercentage,averageViewDuration,impressionClickThroughRate" +
                  $"&dimensions=video" +
                  $"&filters={Uri.EscapeDataString(ids)}";

        var client = httpClientFactory.CreateClient("YouTube");
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("YouTube Analytics API returned {StatusCode}", response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var report = JsonSerializer.Deserialize<AnalyticsReport>(json, JsonOptions);

            if (report?.Rows is null) return [];

            var result = new Dictionary<string, VideoAnalyticsData>();
            // Column order: video, views, estimatedMinutesWatched, averageViewPercentage, averageViewDuration, impressionClickThroughRate
            foreach (var row in report.Rows)
            {
                if (row.Count < 6) continue;
                var videoId = row[0].ToString()!;
                result[videoId] = new VideoAnalyticsData(
                    videoId,
                    row[1] is JsonElement v ? v.GetInt64() : null,
                    row[2] is JsonElement w ? w.GetDouble() : null,
                    row[3] is JsonElement p ? p.GetDouble() : null,
                    row[4] is JsonElement d ? d.GetDouble() : null,
                    row[5] is JsonElement ctr ? ctr.GetDouble() : null);
            }
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "YouTube Analytics API call failed");
            return [];
        }
    }

    private async Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken, CancellationToken ct)
    {
        var clientId = config["Google:ClientId"];
        var clientSecret = config["Google:ClientSecret"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret)) return null;

        var client = httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["client_id"]     = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken
        });

        try
        {
            var response = await client.PostAsync(TokenEndpoint, content, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<TokenResponse>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Token refresh failed");
            return null;
        }
    }

    private sealed record AnalyticsReport(
        [property: JsonPropertyName("rows")] List<List<object>>? Rows);

    internal sealed record TokenResponse(
        [property: JsonPropertyName("access_token")]  string AccessToken,
        [property: JsonPropertyName("expires_in")]    int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
}
