using System.Text.Json;

namespace StrategicoAudit.Api.Models;

public class AuditEvent
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    // Who
    public long ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRole { get; set; }

    // Trace / origin
    public string Source { get; set; } = "ADMIN_UI"; // ADMIN_UI | SUPPORT_TOOL | SYSTEM_JOB | API
    public string RequestId { get; set; } = default!;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // What
    public string Action { get; set; } = default!;     // INVENTORY_ADJUST, ORDER_OVERRIDE, ...
    public string EntityType { get; set; } = default!; // Order, InventoryItem, Shipment...
    public string EntityId { get; set; } = default!;   // store as string for flexibility

    // Outcome
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }

    // Details
    public JsonDocument? Metadata { get; set; } // stored as jsonb
    public JsonDocument? Changes { get; set; }  // stored as jsonb (diff old/new)
}
