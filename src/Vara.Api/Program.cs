using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Vara.Api.Data;
using Vara.Api.Endpoints;
using Vara.Api.Services.Auth;

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

    // Services
    builder.Services.AddScoped<TokenService>();

    var app = builder.Build();

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

    // Serilog request logging (replaces default ASP.NET Core request logs)
    app.UseSerilogRequestLogging();

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // Health check
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    // Auth endpoints: POST /api/auth/register, POST /api/auth/login
    app.MapGroup("/api/auth").MapAuthEndpoints();

    // User endpoints: GET /api/users/me (requires JWT)
    app.MapGroup("/api/users").MapUserEndpoints();

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
