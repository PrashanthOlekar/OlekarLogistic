using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Common;
using ProCargo.Api.Contracts;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;
using ProCargo.Api.Domain;
using ProCargo.Api.Services;

namespace ProCargo.Api.Endpoints.Admin;

/// <summary>
/// Accounts and the audit trail.
///   GET  /api/admin/users?role=Owner&amp;q=ravi   search accounts
///   POST /api/admin/users/{id}/block            block or unblock an account
///   GET  /api/admin/audit                       the last 100 recorded actions
/// </summary>
public static class AdminUserEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        RouteGroupBuilder group = api.MapGroup("/admin").RequireAuthorization(Roles.Admin);

        group.MapGet("/users", SearchUsersAsync);
        group.MapPost("/users/{id:long}/block", SetBlockedAsync);
        group.MapGet("/audit", ListAuditTrailAsync);
    }

    /// <param name="role">Customer, Owner, Driver or Admin. Leave empty for everyone.</param>
    /// <param name="q">Part of a name or mobile number.</param>
    private static async Task<IResult> SearchUsersAsync(string? role, string? q, ProCargoDbContext db)
    {
        var users = await db.Users
            .Where(user => role == null || user.Role == role)
            .Where(user => q == null || user.FullName.Contains(q) || user.Mobile.Contains(q))
            .OrderByDescending(user => user.CreatedAt)
            .Take(200)
            .Select(user => new
            {
                id = user.UserId,
                user.Role,
                user.FullName,
                user.Mobile,
                user.Email,
                user.Status,
                user.CreatedAt,
                user.LastLoginAt,
            })
            .ToListAsync();

        return Results.Ok(users);
    }

    private static async Task<IResult> SetBlockedAsync(
        long id,
        BlockUserRequest request,
        ClaimsPrincipal principal,
        ProCargoDbContext db,
        HttpContext http)
    {
        Guard.Require(id != principal.GetUserId(), "You can't block your own account.");

        User user = await db.Users.FindAsync(id)
            ?? throw ApiException.NotFound("User not found.");

        user.Status = request.Block ? UserStatus.Blocked : UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        AuditLogger.Log(db, http, request.Block ? "User.Blocked" : "User.Unblocked", "User", id);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> ListAuditTrailAsync(ProCargoDbContext db)
    {
        var entries = await db.AuditLogs
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(100)
            .Select(entry => new
            {
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.ActorUserId,
                entry.NewValues,
                entry.CreatedAt,
            })
            .ToListAsync();

        return Results.Ok(entries);
    }
}
