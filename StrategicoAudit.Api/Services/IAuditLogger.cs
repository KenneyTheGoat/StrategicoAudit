using System.Text.Json;

namespace StrategicoAudit.Api.Services;

public record AuditLogRequest(
    long ActorUserId,
    string Action,
    string EntityType,
    string EntityId,
    string Source,
    JsonDocument? Metadata,
    JsonDocument? Changes,
    bool Success = true,
    string? ErrorMessage = null,
    string? ActorName = null,
    string? ActorRole = null
);

public interface IAuditLogger
{
    Task LogAsync(HttpContext http, AuditLogRequest req, CancellationToken ct = default);
}
