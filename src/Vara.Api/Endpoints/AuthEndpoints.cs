using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;
using Vara.Api.Filters;
using Vara.Api.Models.Entities;
using Vara.Api.Services.Auth;

namespace Vara.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
            .WithTags("Auth")
            .WithSummary("Register a new user");
        group.MapPost("/login", Login)
            .WithTags("Auth")
            .WithSummary("Log in with email and password");
        group.MapPost("/refresh", Refresh)
            .WithTags("Auth")
            .WithSummary("Exchange a refresh token for a new JWT");
        group.MapPost("/logout", Logout)
            .WithTags("Auth")
            .WithSummary("Revoke a refresh token");
        return group;
    }

    private static async Task<IResult> Register(
        RegisterRequest req,
        VaraContext db,
        TokenService tokenService)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return Results.Conflict(new { error = "Email already registered." });

        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FullName = req.FullName
        };

        SetRefreshToken(user, tokenService);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(user);
        return Results.Created($"/api/users/{user.Id}",
            new AuthResponse(token, user.RefreshToken!, user.Id, user.Email));
    }

    private static async Task<IResult> Login(
        LoginRequest req,
        VaraContext db,
        TokenService tokenService)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Results.Unauthorized();

        SetRefreshToken(user, tokenService);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.RefreshToken!, user.Id, user.Email));
    }

    private static async Task<IResult> Refresh(
        RefreshRequest req,
        VaraContext db,
        TokenService tokenService)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.RefreshToken == req.RefreshToken);

        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Results.Unauthorized();

        // Rotate: issue new refresh token on every use
        SetRefreshToken(user, tokenService);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.RefreshToken!, user.Id, user.Email));
    }

    private static async Task<IResult> Logout(
        LogoutRequest req,
        VaraContext db)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.RefreshToken == req.RefreshToken);

        if (user is not null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await db.SaveChangesAsync();
        }

        // Always return 204 — don't reveal whether the token existed
        return Results.NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static void SetRefreshToken(User user, TokenService tokenService)
    {
        user.RefreshToken = tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
    }
}

public record RegisterRequest(string Email, string Password, string? FullName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record AuthResponse(string Token, string RefreshToken, Guid UserId, string Email);
