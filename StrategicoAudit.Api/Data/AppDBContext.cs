using Microsoft.EntityFrameworkCore;
using StrategicoAudit.Api.Models;

namespace StrategicoAudit.Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    
    public DbSet<StrategicoAudit.Api.Models.InventoryItem> InventoryItems => Set<StrategicoAudit.Api.Models.InventoryItem>();



    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<StrategicoAudit.Api.Models.InventoryItem>(e =>
        {
            e.ToTable("inventory_item");
            e.HasKey(x => x.Id);

            e.Property(x => x.WarehouseId).HasColumnName("warehouse_id").HasMaxLength(50).IsRequired();
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(80).IsRequired();
            e.Property(x => x.OnHand).HasColumnName("on_hand").IsRequired();
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()").IsRequired();

            e.HasIndex(x => new { x.WarehouseId, x.Sku }).IsUnique();
        });


        var e = modelBuilder.Entity<AuditEvent>();

        e.ToTable("audit_event");
        e.HasKey(x => x.Id);

        e.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        e.Property(x => x.ActorUserId).HasColumnName("actor_user_id").IsRequired();
        e.Property(x => x.ActorName).HasColumnName("actor_name");
        e.Property(x => x.ActorRole).HasColumnName("actor_role");

        e.Property(x => x.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
        e.Property(x => x.RequestId).HasColumnName("request_id").HasMaxLength(100).IsRequired();
        e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        e.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(300);

        e.Property(x => x.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
        e.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(80).IsRequired();
        e.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(80).IsRequired();

        e.Property(x => x.Success).HasColumnName("success").HasDefaultValue(true).IsRequired();
        e.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(500);

        // Map JSON -> jsonb in Postgres
        e.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        e.Property(x => x.Changes).HasColumnName("changes").HasColumnType("jsonb");

        // Indexes
        e.HasIndex(x => new { x.ActorUserId, x.OccurredAt }).HasDatabaseName("idx_audit_actor_time");
        e.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt }).HasDatabaseName("idx_audit_entity_time");
        e.HasIndex(x => x.RequestId).HasDatabaseName("idx_audit_request");
        e.HasIndex(x => new { x.Action, x.OccurredAt }).HasDatabaseName("idx_audit_action_time");
    }
}
