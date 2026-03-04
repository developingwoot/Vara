using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Models.Entities;
using Vara.Api.Services.YouTube;

namespace Vara.Api.Endpoints;

public static class YouTubeOAuthEndpoints
{
    private const string Scope = "https://www.googleapis.com/auth/yt-analytics.readonly https://www.googleapis.com/auth/youtube.readonly";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    public static RouteGroupBuilder MapYouTubeOAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/connect", Connect)
            .WithTags("YouTube OAuth")
            .WithSummary("Redirect user to Google OAuth consent screen")
            .RequireAuthorization();

        group.MapGet("/callback", Callback)
            .WithTags("YouTube OAuth")
            .WithSummary("Handle Google OAuth callback and store tokens")
            .AllowAnonymous();    // JWT unavailable during redirect

        group.MapDelete("/disconnect", Disconnect)
            .WithTags("YouTube OAuth")
            .WithSummary("Revoke and delete the stored YouTube Analytics token")
            .RequireAuthorization();

        group.MapGet("/status", Status)
            .WithTags("YouTube OAuth")
            .WithSummary("Check if the user has connected YouTube Analytics")
            .RequireAuthorization();

        return group;
    }

    // ── GET /api/youtube/oauth/connect ────────────────────────────────────────

    private static IResult Connect(
        ClaimsPrincipal principal,
        IConfiguration config,
        HttpContext context)
    {
        var userId = GetUserId(principal);
        var clientId = config["Google:ClientId"];
        var redirectUri = config["Google:RedirectUri"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            return Results.Problem("Google OAuth is not configured on this server.");

        // Encode userId in state for CSRF + identity recovery at callback
        var state = BuildState(userId, config["Jwt:Secret"]!);

        var authUrl = $"{AuthEndpoint}?" +
            $"client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(Scope)}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&state={Uri.EscapeDataString(state)}";

        return Results.Ok(new { url = authUrl });
    }

    // ── GET /api/youtube/oauth/callback ──────────────────────────────────────

    private static async Task<IResult> Callback(
        string? code,
        string? state,
        string? error,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        VaraContext db,
        CancellationToken ct)
    {
        var frontendBase = config["Frontend:BaseUrl"] ?? "http://localhost:5173";

        if (!string.IsNullOrEmpty(error))
            return Results.Redirect($"{frontendBase}/settings/account?yt_error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Results.Redirect($"{frontendBase}/settings/account?yt_error=missing_params");

        // Verify state and extract userId
        var userId = VerifyState(state, config["Jwt:Secret"]!);
        if (userId is null)
            return Results.Redirect($"{frontendBase}/settings/account?yt_error=invalid_state");

        // Exchange code for tokens
        var clientId = config["Google:ClientId"]!;
        var clientSecret = config["Google:ClientSecret"]!;
        var redirectUri = config["Google:RedirectUri"]!;

        var httpClient = httpClientFactory.CreateClient();
        var tokenResponse = await ExchangeCodeAsync(httpClient, code, clientId, clientSecret, redirectUri, ct);

        if (tokenResponse is null)
            return Results.Redirect($"{frontendBase}/settings/account?yt_error=token_exchange_failed");

        // Get the user's YouTube channel ID
        var channelId = await GetYouTubeChannelIdAsync(httpClient, tokenResponse.AccessToken, ct);

        // Upsert token (one token per user)
        var existing = await db.YouTubeOAuthTokens
            .FirstOrDefaultAsync(t => t.UserId == userId.Value, ct);

        if (existing is null)
        {
            db.YouTubeOAuthTokens.Add(new YouTubeOAuthToken
            {
                UserId = userId.Value,
                YoutubeChannelId = channelId ?? "",
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30)
            });
        }
        else
        {
            existing.AccessToken = tokenResponse.AccessToken;
            if (tokenResponse.RefreshToken is not null)
                existing.RefreshToken = tokenResponse.RefreshToken;
            existing.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);
            existing.YoutubeChannelId = channelId ?? existing.YoutubeChannelId;
            existing.ConnectedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return Results.Redirect($"{frontendBase}/channels?yt_connected=true");
    }

    // ── DELETE /api/youtube/oauth/disconnect ──────────────────────────────────

    private static async Task<IResult> Disconnect(
        ClaimsPrincipal principal,
        VaraContext db,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        var token = await db.YouTubeOAuthTokens
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);

        if (token is not null)
        {
            db.YouTubeOAuthTokens.Remove(token);
            await db.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }

    // ── GET /api/youtube/oauth/status ─────────────────────────────────────────

    private static async Task<IResult> Status(
        ClaimsPrincipal principal,
        IYouTubeAnalyticsClient analyticsClient,
        CancellationToken ct)
    {
        var userId = GetUserId(principal);
        var connected = await analyticsClient.IsConnectedAsync(userId, ct);
        return Results.Ok(new { connected });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildState(Guid userId, string secret)
    {
        var payload = Convert.ToBase64String(userId.ToByteArray());
        var sig = ComputeHmac(payload, secret);
        return $"{payload}.{sig}";
    }

    private static Guid? VerifyState(string state, string secret)
    {
        var parts = state.Split('.', 2);
        if (parts.Length != 2) return null;

        var expectedSig = ComputeHmac(parts[0], secret);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(parts[1]),
            Encoding.UTF8.GetBytes(expectedSig)))
            return null;

        try
        {
            return new Guid(Convert.FromBase64String(parts[0]));
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeHmac(string data, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hash);
    }

    private static async Task<OAuthTokenResponse?> ExchangeCodeAsync(
        HttpClient client, string code, string clientId, string clientSecret,
        string redirectUri, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["client_id"]     = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"]  = redirectUri
        });

        try
        {
            var response = await client.PostAsync(TokenEndpoint, content, ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<OAuthTokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    private static async Task<string?> GetYouTubeChannelIdAsync(HttpClient client, string accessToken, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://www.googleapis.com/youtube/v3/channels?part=id&mine=true");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("items")[0]
                .GetProperty("id")
                .GetString();
        }
        catch { return null; }
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(
            principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    private sealed record OAuthTokenResponse(
        [property: JsonPropertyName("access_token")]  string AccessToken,
        [property: JsonPropertyName("expires_in")]    int ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("token_type")]    string TokenType);
}
