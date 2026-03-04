using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Middleware;
using Vara.Api.Models;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using Scalar.AspNetCore;
using Serilog;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.OpenApi;
using Polly;
using Vara.Api.Data;
using Vara.Api.Endpoints;
using Vara.Api.Plugins;
using Vara.Api.Plugins.OutlierDetection;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Auth;
using Vara.Api.Services.Background;
using Vara.Api.Services.Monitoring;
using Vara.Api.Services.Llm;
using Vara.Api.Services.Plugins;
using Vara.Api.Services.YouTube;
using Vara.Api.Services;
using Vara.Api.Validators;
using Vara.Api.Hubs;

// Bootstrap logger captures startup errors before full config is loaded
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — reads config from appsettings.json under "Serilog" key
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Database
    builder.Services.AddDbContext<VaraContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"]!;
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
            // WebSocket connections can't set HTTP headers — pass token via query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) &&
                        context.HttpContext.Request.Path.StartsWithSegments("/api/hub"))
                        context.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", policy => policy.RequireClaim("admin", "true"));
    });
    builder.Services.AddSignalR();

    // CORS — AllowCredentials is required for SignalR WebSocket; incompatible with AllowAnyOrigin
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    // Rate limiting
    // GlobalLimiter applies to every request. Per-endpoint policies stack on top for stricter limits.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (ctx, ct) =>
        {
            ctx.HttpContext.Response.ContentType = "application/json";
            await ctx.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Code    = "RATE_LIMIT_EXCEEDED",
                Message = "Too many requests. Please slow down and try again.",
                TraceId = ctx.HttpContext.TraceIdentifier
            }, ct);
        };

        // Global baseline: 60 requests/minute per authenticated user, or per IP for anonymous traffic.
        // Applies automatically to all endpoints; no RequireRateLimiting needed on each group.
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            var key = ctx.User?.FindFirst("sub")?.Value
                   ?? ctx.Connection.RemoteIpAddress?.ToString()
                   ?? "unknown";
            return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
            {
                Window            = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                PermitLimit       = 60,
                QueueLimit        = 0
            });
        });

        // Auth endpoints: additionally limited to 10/min per IP (brute-force protection)
        options.AddPolicy("auth", ctx =>
            RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    Window            = TimeSpan.FromMinutes(1),
                    PermitLimit       = 10,
                    QueueLimit        = 0,
                    AutoReplenishment = true
                }));

        // Plugin execution: additionally limited to 10 executions/minute per user
        options.AddPolicy("plugin-execute", ctx =>
        {
            var key = ctx.User?.FindFirst("sub")?.Value
                   ?? ctx.Connection.RemoteIpAddress?.ToString()
                   ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                Window            = TimeSpan.FromMinutes(1),
                PermitLimit       = 10,
                QueueLimit        = 0,
                AutoReplenishment = true
            });
        });
    });

    // OpenAPI
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    // Validators
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

    // In-memory cache (used by VideoCache)
    builder.Services.AddMemoryCache();

    // YouTube HTTP client — named "YouTube", with retry + timeout resilience
    builder.Services.AddHttpClient("YouTube")
        .AddResilienceHandler("youtube", pipeline =>
        {
            // Timeout per attempt
            pipeline.AddTimeout(TimeSpan.FromSeconds(15));

            // Retry up to 3 times with exponential backoff + jitter.
            // Handles transient HTTP errors and 429 / 5xx responses.
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => r.StatusCode is
                        System.Net.HttpStatusCode.TooManyRequests or
                        System.Net.HttpStatusCode.InternalServerError or
                        System.Net.HttpStatusCode.BadGateway or
                        System.Net.HttpStatusCode.ServiceUnavailable or
                        System.Net.HttpStatusCode.GatewayTimeout)
            });
        });

    // LLM HTTP clients — named per provider, 30s timeout, 2 retries
    foreach (var clientName in new[] { "Anthropic", "OpenAI", "Groq" })
    {
        builder.Services.AddHttpClient(clientName)
            .AddResilienceHandler(clientName.ToLower(), pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(30));
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r => r.StatusCode is
                            System.Net.HttpStatusCode.TooManyRequests or
                            System.Net.HttpStatusCode.InternalServerError or
                            System.Net.HttpStatusCode.ServiceUnavailable)
                });
            });
    }

    // LLM providers — registered only when API key is configured
    if (!string.IsNullOrEmpty(builder.Configuration["Llm:Providers:Anthropic:ApiKey"]))
        builder.Services.AddScoped<ILlmProvider, AnthropicProvider>();
    if (!string.IsNullOrEmpty(builder.Configuration["Llm:Providers:OpenAi:ApiKey"]))
        builder.Services.AddScoped<ILlmProvider, OpenAiProvider>();
    if (!string.IsNullOrEmpty(builder.Configuration["Llm:Providers:Groq:ApiKey"]))
        builder.Services.AddScoped<ILlmProvider, GroqProvider>();

    builder.Services.AddScoped<ILlmOrchestrator, LlmOrchestrator>();

    // YouTube services
    builder.Services.AddScoped<ITranscriptFetcher, TranscriptFetcher>();
    builder.Services.AddScoped<IYouTubeAnalyticsClient, YouTubeAnalyticsClient>();
    builder.Services.AddScoped<YouTubeClient>();

    // VideoCache wraps YouTubeClient — resolve IYouTubeClient as the cached version
    builder.Services.AddScoped<IYouTubeClient>(sp => new VideoCache(
        sp.GetRequiredService<YouTubeClient>(),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        sp.GetRequiredService<ILogger<VideoCache>>()
    ));

    // Services
    builder.Services.AddScoped<TokenService>();
    builder.Services.AddScoped<IKeywordAnalyzer, KeywordAnalyzer>();
    builder.Services.AddScoped<IVideoAnalyzer, VideoAnalyzer>();
    builder.Services.AddScoped<ITrendDetector, TrendDetectionService>();
    builder.Services.AddScoped<IPlanEnforcer, PlanEnforcer>();
    builder.Services.AddScoped<IUsageMeter, UsageMeter>();
    builder.Services.AddScoped<IEnhancedKeywordAnalyzer, EnhancedKeywordAnalyzerService>();
    builder.Services.AddScoped<ITranscriptAnalysisService, TranscriptAnalysisService>();
    builder.Services.AddScoped<IChannelAuditService, ChannelAuditService>();
    builder.Services.AddSingleton<BackgroundJobHealthMonitor>();
    builder.Services.AddHostedService<TrendAnalysisBackgroundService>();

    // Plugin system
    var pluginRegistry = new PluginRegistry();
    pluginRegistry.Register(new OutlierDetectionPlugin());
    builder.Services.AddSingleton(pluginRegistry);
    builder.Services.AddScoped<PluginDiscoveryService>();
    builder.Services.AddScoped<PluginExecutionService>();
    builder.Services.AddScoped<INicheComparisonService, NicheComparisonService>();
    builder.Services.AddScoped<INicheNormalizationService, NicheNormalizationService>();

    var app = builder.Build();

    // Apply pending migrations, seed initial data, and discover plugins on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<VaraContext>();
        await db.Database.MigrateAsync();
        await SeedData.SeedInitialKeywordsAsync(db);
        await SeedData.SeedCanonicalNichesAsync(db);
        await SeedData.SeedAdminUsersAsync(db);

        var discoveryService = scope.ServiceProvider.GetRequiredService<PluginDiscoveryService>();
        var pluginsDir = Path.GetFullPath(
            Path.Combine(app.Environment.ContentRootPath,
                         builder.Configuration["Plugins:Directory"] ?? "../../plugins"));
        await discoveryService.DiscoverAsync(pluginsDir);
    }

    // Global exception handler middleware
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // OpenAPI spec + Scalar UI
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

    // Serilog request logging (replaces default ASP.NET Core request logs)
    app.UseSerilogRequestLogging();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    // Health check — exempt from rate limiting so uptime monitors are never blocked
    app.MapGet("/health", async (BackgroundJobHealthMonitor jobHealth, VaraContext db) =>
    {
        var dbOk = false;
        try { dbOk = await db.Database.CanConnectAsync(); } catch { /* db not ready */ }

        return Results.Ok(new
        {
            status = dbOk ? "healthy" : "degraded",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            database = dbOk ? "connected" : "unavailable",
            backgroundJobs = jobHealth.GetAll()
        });
    })
    .WithTags("System")
    .WithSummary("Health check including DB connectivity and background job status")
    .DisableRateLimiting();

    // Auth endpoints: POST /api/auth/register, POST /api/auth/login
    app.MapGroup("/api/auth").MapAuthEndpoints().RequireRateLimiting("auth");

    // User endpoints: GET /api/users/me (requires JWT)
    app.MapGroup("/api/users").MapUserEndpoints();

    // Channel endpoints: POST|GET /api/channels, GET|POST|DELETE /api/channels/{id}
    app.MapGroup("/api/channels").RequireAuthorization().MapChannelEndpoints();

    // Channel audit endpoints: GET /api/channels/{id}/quick-scan, POST /api/channels/{id}/deep-audit, POST /api/channels/videos/compare
    app.MapGroup("/api/channels").RequireAuthorization().MapChannelAuditEndpoints();

    // YouTube Analytics OAuth: GET /api/youtube/oauth/connect|callback|status, DELETE /api/youtube/oauth/disconnect
    app.MapGroup("/api/youtube/oauth").MapYouTubeOAuthEndpoints();

    // Video endpoints: GET /api/videos/search, POST|GET /api/videos, GET|DELETE /api/videos/{id}
    app.MapGroup("/api/videos").RequireAuthorization().MapVideoEndpoints();

    // Keyword endpoints: POST|GET /api/keywords, GET|DELETE /api/keywords/{id}
    app.MapGroup("/api/keywords").RequireAuthorization().MapKeywordEndpoints();

    // Video analysis endpoints: POST /api/analysis/videos, POST /api/analysis/videos/export
    app.MapGroup("/api/analysis/videos").RequireAuthorization().MapVideoAnalysisEndpoints();

    // Trend analysis endpoints: GET /api/analysis/trends
    app.MapGroup("/api/analysis/trends").RequireAuthorization().MapTrendAnalysisEndpoints();

    // LLM endpoints: POST /api/llm/generate
    app.MapGroup("/api/llm").RequireAuthorization().MapLlmEndpoints();

    // Plugin endpoints: GET|POST /api/plugins/...
    app.MapGroup("/api/plugins").RequireAuthorization().MapPluginEndpoints();

    // Niche analysis: POST /api/analysis/niche/compare
    app.MapGroup("/api/analysis/niche").RequireAuthorization().MapNicheEndpoints();

    // Canonical niches: GET /api/niches, POST /api/niches/resolve
    app.MapGroup("/api/niches").RequireAuthorization().MapNicheListEndpoints();

    // Admin: CRUD for canonical niches
    app.MapGroup("/api/admin/niches").RequireAuthorization("Admin").MapAdminNicheEndpoints();

    // Admin: LLM cost reporting
    app.MapGroup("/api/admin/costs").RequireAuthorization("Admin").MapAdminCostEndpoints();

    // SignalR hub: ws /api/hub/analysis
    app.MapHub<AnalysisHub>("/api/hub/analysis");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = securitySchemes;
        }
    }
}
