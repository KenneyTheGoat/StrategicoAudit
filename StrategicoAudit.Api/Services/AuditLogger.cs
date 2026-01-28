using StrategicoAudit.Api.Data;
using StrategicoAudit.Api.Models;

namespace StrategicoAudit.Api.Services;

public class AuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;

    public AuditLogger(AppDbContext db) => _db = db;

    public async Task LogAsync(HttpContext http, AuditLogRequest req, CancellationToken ct = default)
    {
        var requestId = http.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N");
        var ip = http.Items["IpAddress"]?.ToString();
        var ua = http.Items["UserAgent"]?.ToString();

        var ev = new AuditEvent
        {
            ActorUserId = req.ActorUserId,
            ActorName = req.ActorName,
            ActorRole = req.ActorRole,

            Source = req.Source,
            RequestId = requestId,
            IpAddress = ip,
            UserAgent = ua,

            Action = req.Action,
            EntityType = req.EntityType,
            EntityId = req.EntityId,

            Success = req.Success,
            ErrorMessage = req.ErrorMessage,

            Metadata = req.Metadata,
            Changes = req.Changes
        };

        _db.AuditEvents.Add(ev);

        // IMPORTANT: do not call SaveChanges here if you want strict transactional coupling.
        // We will SaveChanges in the endpoint's transaction scope.
        await Task.CompletedTask;
    }
}
