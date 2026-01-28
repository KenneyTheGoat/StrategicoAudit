using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrategicoAudit.Api.Data;
using StrategicoAudit.Api.Helpers;
using StrategicoAudit.Api.Models;
using StrategicoAudit.Api.Services;

namespace StrategicoAudit.Api.Pages.User;

public class InventoryAdjustModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IAuditLogger _audit;

    public InventoryAdjustModel(AppDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    [BindProperty] public string WarehouseId { get; set; } = "W1";
    [BindProperty] public string Sku { get; set; } = "ABC123";
    [BindProperty] public int NewOnHand { get; set; } = 110;
    [BindProperty] public string? Reason { get; set; } = "Damaged";

    public string ActorDisplay { get; set; } = "Unknown";
    public string? Message { get; set; }

    private (long userId, string name, string role) GetActor()
    {
        if (!Request.Cookies.TryGetValue("demo_actor", out var json) || string.IsNullOrWhiteSpace(json))
            return (1001, "Warehouse User", "USER");

        try
        {
            var actor = JsonSerializer.Deserialize<StrategicoAudit.Api.Pages.DemoActor>(json);
            if (actor == null) return (1001, "Warehouse User", "USER");
            return (actor.UserId, actor.Name, actor.Role);
        }
        catch
        {
            return (1001, "Warehouse User", "USER");
        }
    }

    public void OnGet()
    {
        var actor = GetActor();
        ActorDisplay = $"{actor.name} ({actor.role}, id={actor.userId})";
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var actor = GetActor();
        ActorDisplay = $"{actor.name} ({actor.role}, id={actor.userId})";

        var entityId = $"{WarehouseId}:{Sku}";

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // Ensure record exists (demo-friendly)
            var item = await _db.InventoryItems
                .FirstOrDefaultAsync(x => x.WarehouseId == WarehouseId && x.Sku == Sku, ct);

            if (item == null)
            {
                item = new InventoryItem { WarehouseId = WarehouseId, Sku = Sku, OnHand = 0 };
                _db.InventoryItems.Add(item);
                await _db.SaveChangesAsync(ct); // get it persisted for old value correctness
            }

            var oldValues = new Dictionary<string, object?> { ["on_hand"] = item.OnHand };
            item.OnHand = NewOnHand;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            var newValues = new Dictionary<string, object?> { ["on_hand"] = item.OnHand };

            var changes = JsonHelpers.Diff(oldValues, newValues);

            var metadata = JsonHelpers.MaskSensitive(new Dictionary<string, object?>
            {
                ["warehouse_id"] = WarehouseId,
                ["sku"] = Sku,
                ["reason"] = Reason
            });

            // Log audit inside same transaction
            await _audit.LogAsync(HttpContext, new AuditLogRequest(
                ActorUserId: actor.userId,
                ActorName: actor.name,
                ActorRole: actor.role,
                Action: "INVENTORY_ADJUST",
                EntityType: "InventoryItem",
                EntityId: entityId,
                Source: "ADMIN_UI",
                Metadata: JsonHelpers.ToJsonDocument(metadata),
                Changes: JsonHelpers.ToJsonDocument(changes),
                Success: true
            ), ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            Message = $"OK — Updated inventory and logged audit event for {entityId}.";
            return Page();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            Message = $"Failed: {ex.Message}";
            return Page();
        }
    }
}
