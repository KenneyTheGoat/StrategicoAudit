using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using StrategicoAudit.Api.Data;
using StrategicoAudit.Api.Dtos;
using StrategicoAudit.Api.Helpers;
using StrategicoAudit.Api.Middleware;
using StrategicoAudit.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ----
builder.Services.AddControllers();

// Swagger (API docs)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Razor Pages (UI)
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Db");
    opt.UseNpgsql(cs);
});

// App services
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

var app = builder.Build();

// ---- Middleware / pipeline ----
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Adds RequestId / IP / UserAgent and echoes X-Request-Id back
app.UseMiddleware<RequestContextMiddleware>();

// Static files (optional; warning is fine if wwwroot doesn't exist)
app.UseStaticFiles();

app.UseRouting();

// ---- UI routes ----
app.MapRazorPages();

// Simple health endpoint (keep / free for UI homepage)
app.MapGet("/health", () => Results.Ok(new { status = "OK" }));

// ---- DEMO: Admin action endpoint (inventory adjust) ----
// NOTE: This demo does NOT include a real inventory table yet.
// It demonstrates transaction pattern + audit logging.
app.MapPost("/admin/inventory/adjust", async (
    HttpContext http,
    AppDbContext db,
    IAuditLogger audit,
    InventoryAdjustDto dto,
    CancellationToken ct) =>
{
    // Dummy authenticated admin actor
    var actorUserId = 1001L;
    var actorName = "Support Desk Agent";
    var actorRole = "SUPPORT";

    // Old vs new example (in real system, old comes from DB)
    var oldValues = new Dictionary<string, object?>
    {
        ["on_hand"] = 120,
        ["reason"] = null
    };

    var newValues = new Dictionary<string, object?>
    {
        ["on_hand"] = dto.NewOnHand,
        ["reason"] = dto.Reason
    };

    var changes = JsonHelpers.Diff(oldValues, newValues);

    var metadata = JsonHelpers.MaskSensitive(new Dictionary<string, object?>
    {
        ["warehouse_id"] = dto.WarehouseId,
        ["sku"] = dto.Sku,
        ["operation"] = "INVENTORY_ADJUST",
        ["reason"] = dto.Reason
    });

    // Transaction ensures "no change without audit" / "no audit without change"
    await using var tx = await db.Database.BeginTransactionAsync(ct);

    try
    {
        // TODO: update Inventory table here 

        await audit.LogAsync(http, new AuditLogRequest(
            ActorUserId: actorUserId,
            ActorName: actorName,
            ActorRole: actorRole,
            Action: "INVENTORY_ADJUST",
            EntityType: "InventoryItem",
            EntityId: $"{dto.WarehouseId}:{dto.Sku}",
            Source: "ADMIN_UI",
            Metadata: JsonHelpers.ToJsonDocument(metadata),
            Changes: JsonHelpers.ToJsonDocument(changes),
            Success: true
        ), ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Results.Ok(new
        {
            message = "Adjusted (demo). Audit logged.",
            requestId = http.Response.Headers["X-Request-Id"].ToString()
        });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync(ct);
        return Results.Problem("Adjustment failed: " + ex.Message);
    }
});

// ---- Queries (API) ----
app.MapGet("/admin/audit/user/{userId:long}", async (AppDbContext db, long userId) =>
{
    var since = DateTimeOffset.UtcNow.AddDays(-7);

    var rows = await db.AuditEvents
        .Where(x => x.ActorUserId == userId && x.OccurredAt >= since)
        .OrderByDescending(x => x.OccurredAt)
        .Take(200)
        .Select(x => new
        {
            x.OccurredAt,
            x.Action,
            x.EntityType,
            x.EntityId,
            x.Success,
            x.RequestId
        })
        .ToListAsync();

    return Results.Ok(rows);
});

app.MapGet("/admin/audit/entity/{entityType}/{entityId}", async (AppDbContext db, string entityType, string entityId) =>
{
    var rows = await db.AuditEvents
        .Where(x => x.EntityType == entityType && x.EntityId == entityId)
        .OrderByDescending(x => x.OccurredAt)
        .Take(200)
        .Select(x => new
        {
            x.OccurredAt,
            x.ActorUserId,
            x.ActorName,
            x.Action,
            x.Success,
            x.Changes,
            x.Metadata,
            x.RequestId
        })
        .ToListAsync();

    return Results.Ok(rows);
});

// Controllers 
app.MapControllers();

app.Run();
