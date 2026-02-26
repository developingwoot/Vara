using System.Security.Claims;
using Vara.Api.Data;

namespace Vara.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/me", GetCurrentUser).RequireAuthorization();
        return group;
    }

    private static async Task<IResult> GetCurrentUser(ClaimsPrincipal principal, VaraContext db)
    {
        var userId = Guid.Parse(principal.FindFirstValue("sub")!);
        var user = await db.Users.FindAsync(userId);

        if (user is null) return Results.NotFound();

        return Results.Ok(new UserResponse(user.Id, user.Email, user.FullName, user.SubscriptionTier, user.CreatedAt));
    }
}

public record UserResponse(Guid Id, string Email, string? FullName, string SubscriptionTier, DateTime CreatedAt);
