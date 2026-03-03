using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vara.Api.Data;

namespace Vara.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithTags("Users")
            .WithSummary("Get the currently authenticated user");

        group.MapPatch("/me", UpdateCurrentUser)
            .RequireAuthorization()
            .WithTags("Users")
            .WithSummary("Update display name and/or email");

        group.MapPost("/me/change-password", ChangePassword)
            .RequireAuthorization()
            .WithTags("Users")
            .WithSummary("Change account password");

        return group;
    }

    private static async Task<IResult> GetCurrentUser(ClaimsPrincipal principal, VaraContext db)
    {
        var userId = GetUserId(principal);
        var user = await db.Users.FindAsync(userId);

        if (user is null) return Results.NotFound();

        return Results.Ok(new UserResponse(user.Id, user.Email, user.FullName, user.SubscriptionTier, user.IsAdmin, user.CreatedAt));
    }

    private static async Task<IResult> UpdateCurrentUser(
        UpdateUserRequest req,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var user = await db.Users.FindAsync(userId);

        if (user is null) return Results.NotFound();

        if (req.FullName is not null) user.FullName = req.FullName;

        if (req.Email is not null && req.Email != user.Email)
        {
            if (await db.Users.AnyAsync(u => u.Email == req.Email && u.Id != userId))
                return Results.Conflict(new { error = "Email already in use." });
            user.Email = req.Email;
        }

        await db.SaveChangesAsync();
        return Results.Ok(new UserResponse(user.Id, user.Email, user.FullName, user.SubscriptionTier, user.IsAdmin, user.CreatedAt));
    }

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest req,
        ClaimsPrincipal principal,
        VaraContext db)
    {
        var userId = GetUserId(principal);
        var user = await db.Users.FindAsync(userId);

        if (user is null) return Results.NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return Results.UnprocessableEntity(new { error = "Current password is incorrect." });

        if (req.NewPassword.Length < 8)
            return Results.BadRequest(new { error = "New password must be at least 8 characters." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(
            principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));
}

public record UserResponse(Guid Id, string Email, string? FullName, string SubscriptionTier, bool isAdmin, DateTime CreatedAt);
public record UpdateUserRequest(string? FullName, string? Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
