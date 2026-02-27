using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
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
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Auth;
using Vara.Api.Services.YouTube;
using Vara.Api.Validators;

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
        });

    builder.Services.AddAuthorization();

    // CORS (frontend integration)
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

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

    // YouTube services
    builder.Services.AddScoped<ITranscriptFetcher, TranscriptFetcher>();
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

    var app = builder.Build();

    // Apply pending migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<VaraContext>();
        await db.Database.MigrateAsync();
    }

    // Global exception handler — returns JSON instead of HTML stack traces
    app.UseExceptionHandler(exceptionHandlerApp =>
        exceptionHandlerApp.Run(async context =>
        {
            var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
            Log.Error(exceptionFeature?.Error, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
        }));

    // OpenAPI spec + Scalar UI
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

    // Serilog request logging (replaces default ASP.NET Core request logs)
    app.UseSerilogRequestLogging();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // Health check
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithTags("System")
        .WithSummary("Health check");

    // Auth endpoints: POST /api/auth/register, POST /api/auth/login
    app.MapGroup("/api/auth").MapAuthEndpoints();

    // User endpoints: GET /api/users/me (requires JWT)
    app.MapGroup("/api/users").MapUserEndpoints();

    // Channel endpoints: POST|GET /api/channels, GET|POST|DELETE /api/channels/{id}
    app.MapGroup("/api/channels").RequireAuthorization().MapChannelEndpoints();

    // Video endpoints: GET /api/videos/search, POST|GET /api/videos, GET|DELETE /api/videos/{id}
    app.MapGroup("/api/videos").RequireAuthorization().MapVideoEndpoints();

    // Keyword endpoints: POST|GET /api/keywords, GET|DELETE /api/keywords/{id}
    app.MapGroup("/api/keywords").RequireAuthorization().MapKeywordEndpoints();

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
