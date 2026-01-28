using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StrategicoAudit.Api.Data;
using StrategicoAudit.Api.Models;

namespace StrategicoAudit.Api.Pages.Admin.Audit;

public class AuditDashboardModel : PageModel
{
    private readonly AppDbContext _db;

    public AuditDashboardModel(AppDbContext db) => _db = db;

    public string ActorDisplay { get; set; } = "Unknown";
    public string? Message { get; set; }

    public List<AuditEvent> Latest { get; set; } = new();

    // Query 1 inputs/results
    public string UserId { get; set; } = "1001";
    public List<UserActionsRow> UserActionsRows { get; set; } = new();

    // Query 2 inputs/results
    public string EntityType { get; set; } = "InventoryItem";
    public string EntityId { get; set; } = "W1:ABC123";
    public List<EntityHistoryRow> EntityHistoryRows { get; set; } = new();

    public string SqlUserActions => @"                                                      
SELECT occurred_at, action, entity_type, entity_id, success, request_id
FROM audit_event
WHERE actor_user_id = @user_id                                                          
  AND occurred_at >= NOW() - INTERVAL '7 days'
ORDER BY occurred_at DESC;
".Trim();// This can change and more queries can be written this way

    public string SqlEntityHistory => @"
SELECT occurred_at, actor_user_id, action, changes::text AS changes, metadata::text AS metadata, request_id
FROM audit_event
WHERE entity_type = @entity_type
  AND entity_id = @entity_id
ORDER BY occurred_at DESC;
".Trim();

    private (long userId, string name, string role) GetActor()
    {
        if (!Request.Cookies.TryGetValue("demo_actor", out var json) || string.IsNullOrWhiteSpace(json))
            return (9001, "Admin Viewer", "ADMIN");

        try
        {
            var actor = JsonSerializer.Deserialize<StrategicoAudit.Api.Pages.DemoActor>(json);
            if (actor == null) return (9001, "Admin Viewer", "ADMIN");
            return (actor.UserId, actor.Name, actor.Role);
        }
        catch
        {
            return (9001, "Admin Viewer", "ADMIN");
        }
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var actor = GetActor();
        ActorDisplay = $"{actor.name} ({actor.role}, id={actor.userId})";

        Latest = await _db.AuditEvents
            .OrderByDescending(x => x.OccurredAt)
            .Take(25)
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(string queryName, string? userId, string? entityType, string? entityId, CancellationToken ct)
    {
        var actor = GetActor();
        ActorDisplay = $"{actor.name} ({actor.role}, id={actor.userId})";

        // Simple gate: only ADMIN can view dashboard
        if (!string.Equals(actor.role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            Message = "Access denied (demo): switch to Admin mode on /login.";
            await OnGetAsync(ct);
            return Page();
        }

        // keep latest always
        Latest = await _db.AuditEvents.OrderByDescending(x => x.OccurredAt).Take(25).ToListAsync(ct);

        await using var conn = new NpgsqlConnection(_db.Database.GetConnectionString());
        await conn.OpenAsync(ct);

        if (queryName == "user_actions")
        {
            UserId = string.IsNullOrWhiteSpace(userId) ? "1001" : userId;

            await using var cmd = new NpgsqlCommand(SqlUserActions, conn);
            cmd.Parameters.AddWithValue("user_id", long.Parse(UserId));

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                UserActionsRows.Add(new UserActionsRow(
                    reader.GetDateTime(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetBoolean(4),
                    reader.GetString(5)
                ));
            }
        }
        else if (queryName == "entity_history")
        {
            EntityType = string.IsNullOrWhiteSpace(entityType) ? "InventoryItem" : entityType;
            EntityId = string.IsNullOrWhiteSpace(entityId) ? "W1:ABC123" : entityId;

            await using var cmd = new NpgsqlCommand(SqlEntityHistory, conn);
            cmd.Parameters.AddWithValue("entity_type", EntityType);
            cmd.Parameters.AddWithValue("entity_id", EntityId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                EntityHistoryRows.Add(new EntityHistoryRow(
                    reader.GetDateTime(0),
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)
                ));
            }
        }

        return Page();
    }
}

public record UserActionsRow(DateTime OccurredAt, string Action, string EntityType, string EntityId, bool Success, string RequestId);
public record EntityHistoryRow(DateTime OccurredAt, long ActorUserId, string Action, string Changes, string Metadata, string RequestId);
