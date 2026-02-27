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

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(user);
        return Results.Created($"/api/users/{user.Id}", new AuthResponse(token, user.Id, user.Email));
    }

    private static async Task<IResult> Login(
        LoginRequest req,
        VaraContext db,
        TokenService tokenService)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Results.Unauthorized();

        var token = tokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.Id, user.Email));
    }
}

public record RegisterRequest(string Email, string Password, string? FullName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, Guid UserId, string Email);
