using System.Security.Claims;
using System.Text.Json;
using ProCargo.Api.Data;
using ProCargo.Api.Data.Entities;

namespace ProCargo.Api.Services;

/// <summary>
/// Records important actions (approvals, payouts, blocks...) in the AuditLogs table.
/// The row is saved together with the caller's next SaveChangesAsync.
/// </summary>
public static class AuditLogger
{
    public static void Log(
        ProCargoDbContext db,
        HttpContext http,
        string action,
        string entityType,
        long entityId,
        object? details = null)
    {
        string? userIdText = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        long? actorUserId = long.TryParse(userIdText, out long userId) ? userId : null;

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            NewValues = details is null ? null : JsonSerializer.Serialize(details),
            IpAddress = http.Connection.RemoteIpAddress?.ToString(),
        });
    }
}
